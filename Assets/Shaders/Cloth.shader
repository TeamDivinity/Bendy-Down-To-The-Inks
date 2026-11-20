Shader "Custom/URP/FabricWave"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        
        // --- Настройки волны ---
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.5)) = 0.1
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 1.0
        
        // Частота волны по осям (отдельный контроль)
        _FrequencyX ("Frequency X", Range(0, 20)) = 2.0
        _FrequencyZ ("Frequency Z", Range(0, 20)) = 2.0
        
        // --- Вторая волна для детализации (опционально) ---
        _UseSecondWave ("Use Second Wave", Float) = 1.0
        _SecondAmplitude ("Second Amplitude", Range(0, 0.2)) = 0.05
        _SecondFrequencyX ("Second Freq X", Range(0, 30)) = 5.0
        _SecondFrequencyZ ("Second Freq Z", Range(0, 30)) = 5.0
        _SecondSpeed ("Second Speed Multiplier", Range(0.1, 3)) = 1.3
        
        // Настройка закрепления (маска)
        _AnchorMask ("Anchor Top (UV Y)", Range(0, 1)) = 1.0 
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _FOG_EXP _FOG_EXP2 _FOG_LINEAR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : NORMAL;
                float4 tangentWS    : TANGENT;
                float3 positionWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                
                float _WaveAmplitude;
                float _WaveSpeed;
                float _FrequencyX;
                float _FrequencyZ;
                
                float _UseSecondWave;
                float _SecondAmplitude;
                float _SecondFrequencyX;
                float _SecondFrequencyZ;
                float _SecondSpeed;
                
                float _AnchorMask;
            CBUFFER_END

            // --- Функция расчёта смещения (вынесена для переиспользования) ---
            float3 GetWindDisplacement(float3 posOS, float2 uv, float time, 
                                       float amp, float freqX, float freqZ)
            {
                // Маска: 1 = двигается, 0 = закреплено (по UV.y)
                float moveMask = 1.0 - saturate(uv.y * _AnchorMask);
                
                // Основная волна: комбинируем координаты для диагонального распространения
                float wave = sin(time + posOS.x * freqX + posOS.z * freqZ);
                
                return posOS + float3(wave * amp * moveMask, 0, 0);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 pos = input.positionOS.xyz;
                float time = _Time.y * _WaveSpeed;

                // Применяем первую волну
                pos = GetWindDisplacement(pos, input.uv, time, 
                                          _WaveAmplitude, _FrequencyX, _FrequencyZ);
                
                // Добавляем вторую волну для детализации (если включено)
                if (_UseSecondWave > 0.5)
                {
                    float time2 = time * _SecondSpeed;
                    pos = GetWindDisplacement(pos, input.uv, time2, 
                                              _SecondAmplitude, _SecondFrequencyX, _SecondFrequencyZ);
                }

                input.positionOS.xyz = pos;

                // Подготовка данных для PBR
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionCS = vertexInput.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.positionWS = vertexInput.positionWS;

                return output;
            }

            half4 Frag(Varyings input) : SV_TARGET
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                float3 normalWS = normalize(input.normalWS);
                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), inputData.viewDirectionWS);
                inputData.bakedGI = SAMPLE_GI(input.uv, input.normalWS, inputData.viewDirectionWS);

                // PBR: Albedo, Metallic, Smoothness, Occlusion, Emission
                return UniversalFragmentPBR(inputData, color, 0.0, 0.5, 0.0, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
        
        // --- Shadow Pass (с поддержкой анимации) ---
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVert
            #pragma fragment ShadowPassFrag
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _WaveAmplitude, _WaveSpeed, _FrequencyX, _FrequencyZ;
                float _UseSecondWave, _SecondAmplitude, _SecondFrequencyX, _SecondFrequencyZ, _SecondSpeed;
                float _AnchorMask;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            float3 GetWindDisplacement(float3 posOS, float2 uv, float time, 
                                       float amp, float freqX, float freqZ)
            {
                float moveMask = 1.0 - saturate(uv.y * _AnchorMask);
                float wave = sin(time + posOS.x * freqX + posOS.z * freqZ);
                return posOS + float3(wave * amp * moveMask, 0, 0);
            }

            Varyings ShadowPassVert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float time = _Time.y * _WaveSpeed;
                
                pos = GetWindDisplacement(pos, input.uv, time, 
                                          _WaveAmplitude, _FrequencyX, _FrequencyZ);
                
                if (_UseSecondWave > 0.5)
                {
                    float time2 = time * _SecondSpeed;
                    pos = GetWindDisplacement(pos, input.uv, time2, 
                                              _SecondAmplitude, _SecondFrequencyX, _SecondFrequencyZ);
                }
                
                output.positionCS = TransformObjectToHClip(pos);
                return output;
            }
            half4 ShadowPassFrag(Varyings input) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}