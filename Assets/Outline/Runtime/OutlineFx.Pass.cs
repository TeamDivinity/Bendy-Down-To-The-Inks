using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.CommandBufferPool;

//  OutlineFx © NullTale - https://x.com/NullTale/
namespace OutlineFx
{
    public partial class OutlineFxFeature
    {
        private static readonly int s_Alpha   = Shader.PropertyToID("_Alpha");
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_Step    = Shader.PropertyToID("_Step");
        private static readonly int s_Color   = Shader.PropertyToID("_Color");
        private static readonly int s_Solid   = Shader.PropertyToID("_Solid");
        
        private static readonly int s_AlphaTex    = Shader.PropertyToID("_AlphaTex");
        private static readonly int s_AlphaTO = Shader.PropertyToID("_AlphaTO");
        
        private class Pass : ScriptableRenderPass
        {
            public OutlineFxFeature _owner;
            
            private FilteringSettings       _filtering;
            private RenderStateBlock        _override;
            private RenderTarget            _buffer;
            private RTHandle                _output;

            // =======================================================================
            public void Init()
            {
                renderPassEvent = _owner._event;
                _buffer         = new RenderTarget().Allocate(nameof(_buffer));
            }

            public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // allocate resources
                var cmd  = CommandBufferPool.Get(nameof(OutlineFxFeature));
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                if (_owner._attachDepth)
                    desc.depthBufferBits = 32;
                
#pragma warning disable CS0618 // Type or member is obsolete (Unity 6 warning suppression)
                desc.colorFormat = RenderTextureFormat.ARGB32;
#pragma warning restore CS0618 // Type or member is obsolete
                
                _buffer.Get(cmd, desc);
		
                if (_owner._outlineMat == null)
                {
                    // Исправление утечки памяти CommandBuffer
                    CommandBufferPool.Release(cmd);
    			    return;
                }
		        
                _owner._outlineMat.SetFloat(s_Alpha, _owner._alphaCutout);
                _owner._outlineMat.SetFloat(s_Solid, _owner._solid);
                
                if (_owner._solidMask._enabled)
                {
                    var sm = _owner._solidMask;
                    _owner._outlineMat.SetTexture(s_AlphaTex, sm._pattern);
                    var xPeriod = 1f / (sm._velocity.x / 1000f);
                    var yPeriod = 1f / (sm._velocity.y / 1000f);
                    var xOffset = sm._velocity.x == 0 ? 0 : (Time.unscaledTime % xPeriod) / xPeriod * sm._scale;
                    var yOffset = sm._velocity.y == 0 ? 0 : (Time.unscaledTime % yPeriod) / yPeriod * sm._scale;
                    
                    var aspectTex = sm._pattern.width / (float)sm._pattern.height;
                    
                    _owner._outlineMat.SetVector(s_AlphaTO, new Vector4(sm._scale * (Screen.width / (float)Screen.height) / aspectTex, sm._scale, xOffset, yOffset));
                }
                
                if (_owner._output.Enabled == false)
                {
#if UNITY_6000_0_OR_NEWER
                    // URP 17.6+: wrap the built-in camera target because the
                    // ScriptableRenderer cameraColorTargetHandle accessor is
                    // not exposed by this package API.
                    _output = RTHandles.Alloc(
                        new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget),
                        name: "_OutlineFxCameraTarget");
#elif UNITY_2022_1_OR_NEWER
                    _output = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
#pragma warning disable CS0618
                    _output = RTHandles.Alloc(renderingData.cameraData.renderer.cameraColorTarget);
#pragma warning restore CS0618
#endif
                }

                if (_owner._output.Enabled)
                    _output = _alloc(_owner._output.Value);
                
                // render with layer mask
                cmd.SetRenderTarget(_buffer.Handle.nameID);
                cmd.ClearRenderTarget(false, true, Color.clear, 1f);
                
                // Use the outline buffer's own depth attachment; renderer depth-target
                // properties vary across URP versions and may not be exposed.
                cmd.SetRenderTarget(_buffer.Handle);
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                foreach (var inst in _renderers)
                {
                    if (inst == null)
                        continue;
                    
                    var renderer = inst._renderer;
                    var materials = renderer.sharedMaterials; 
                    
                    for (int subMesh = 0; subMesh < materials.Length; subMesh++)
                    {
                        var mat = materials[subMesh];
                        
                        if (mat != null && mat.mainTexture != null)
                            cmd.SetGlobalTexture(s_MainTex, mat.mainTexture);
                        else
                            cmd.SetGlobalTexture(s_MainTex, Texture2D.whiteTexture); 
                        
                        cmd.SetGlobalColor(s_Color, inst.Color);
                        
                        cmd.DrawRenderer(renderer, _owner._outlineMat, subMesh, 0);
                    }
                }
                _renderers.Clear();
                
                cmd.SetGlobalVector(s_Step, _owner._step);
                _blit(_buffer.Handle, _output, _owner._outlineMat, 1);

                _execute();
				
                // -----------------------------------------------------------------------
                void _blit(RTHandle from, RTHandle to, Material mat, int pass = 0)
                {
#if UNITY_2022_2_OR_NEWER || UNITY_6000_0_OR_NEWER
                    // В Unity 6 (URP 17) ScriptableRenderPass.Blit устарел. Используем Blitter.
                    Blitter.BlitCameraTexture(cmd, from, to, mat, pass);
#else
                    OutlineFxFeature._blit(cmd, from, to, mat, pass);
#endif
                }

                void _execute()
                {
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }

            public void FrameCleanup(CommandBuffer cmd)
            {
                _buffer.Release(cmd);
                
#if UNITY_6000_0_OR_NEWER
                // Both the wrapped camera target and custom output are allocated
                // for this pass and must be released.
                if (_output != null)
                    RTHandles.Release(_output);
#elif !UNITY_2022_1_OR_NEWER
#pragma warning disable CS0618
                RTHandles.Release(_output);
#pragma warning restore CS0618
#else
                if (_owner._output.Enabled)
                    RTHandles.Release(_output);
#endif
            }
        }
    }
}