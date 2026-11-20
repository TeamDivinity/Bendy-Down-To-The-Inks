Shader "Custom/UI/InkDissolve_v2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Параметры эффекта
        _NoiseTex ("Ink Noise", 2D) = "gray" {}
        _DissolveAmount ("Dissolve Progress", Range(0, 0.6)) = 0
        _EdgeWidth ("Edge Width (0 = sharp)", Range(0, 0.2)) = 0.05
        _EdgeColor ("Edge Color (alpha = fade)", Color) = (1, 0.85, 0, 1)
        _NoiseScrollSpeed ("Noise Scroll Speed", Vector) = (0.1, 0.05, 0, 0)
        _ScrollTime ("Scroll Time", Float) = 0

        // Стандартные UI-свойства
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex           : SV_POSITION;
                fixed4 color            : COLOR;
                float2 texcoord         : TEXCOORD0;
                float4 worldPosition    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            sampler2D _NoiseTex;
            float _DissolveAmount;
            float _EdgeWidth;
            fixed4 _EdgeColor;
            float4 _NoiseScrollSpeed;
            float _ScrollTime;
            fixed4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 baseColor = tex2D(_MainTex, i.texcoord) * i.color;

                // Анимация UV шума
                float2 noiseUV = i.texcoord + _ScrollTime * _NoiseScrollSpeed.xy;
                half noiseValue = tex2D(_NoiseTex, noiseUV).r;

                // === ЛОГИКА ТРЁХ РЕЖИМОВ ===
                
                // Режим 1: Резкое отсечение (EdgeWidth = 0)
                if (_EdgeWidth < 0.0001)
                {
                    // Простое сравнение без градиента
                    if (noiseValue < _DissolveAmount)
                        discard;
                    
                    // Возвращаем базовый цвет без модификаций
                    #ifdef UNITY_UI_CLIP_RECT
                    baseColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                    #endif
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(baseColor.a - 0.001);
                    #endif
                    return baseColor;
                }
                
                // Режим 2 и 3: Плавный переход (EdgeWidth > 0)
                float edgeStart = _DissolveAmount - _EdgeWidth;
                float edgeEnd = _DissolveAmount;
                
                // Нормализованное значение в диапазоне края (0 = полностью скрыт, 1 = полностью виден)
                float edgeFactor = smoothstep(edgeStart, edgeEnd, noiseValue);
                
                // Отсечение невидимых пикселей
                clip(edgeFactor - 0.001);

                // === РЕЖИМ 2: Плавный фейд в прозрачность (EdgeColor.a ≈ 0) ===
                if (_EdgeColor.a < 0.001)
                {
                    // Интерполируем альфу от 0 до baseColor.a
                    baseColor.a *= edgeFactor;
                }
                // === РЕЖИМ 3: Цветное свечение краёв (EdgeColor.a > 0) ===
                else
                {
                    // Вычисляем интенсивность свечения (максимум в центре края)
                    float glowIntensity = 1.0 - abs(noiseValue - _DissolveAmount + _EdgeWidth * 0.5) / (_EdgeWidth * 0.5);
                    glowIntensity = saturate(glowIntensity);
                    glowIntensity *= _EdgeColor.a;
                    
                    // Смешиваем базовый цвет с цветом свечения
                    baseColor.rgb = lerp(baseColor.rgb, _EdgeColor.rgb, glowIntensity);
                }

                // Обрезка по UI-маскам
                #ifdef UNITY_UI_CLIP_RECT
                baseColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseColor.a - 0.001);
                #endif

                return baseColor;
            }
            ENDCG
        }
    }
}