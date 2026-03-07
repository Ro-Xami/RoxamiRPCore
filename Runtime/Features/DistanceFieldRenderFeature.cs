using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class DistanceFieldRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] 
        private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        DistanceFieldRenderPass m_Pass;
        
        public override void Create()
        {
            m_Pass = new DistanceFieldRenderPass(renderPassEvent, this.name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;
            
            var settings = VolumeManager.instance.stack.GetComponent<DistanceField>();
            if (settings && settings.IsActive())
            {
                m_Pass.settings = settings;
                renderer.EnqueuePass(m_Pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
        }

        class DistanceFieldRenderPass : ScriptableRenderPass
        {
            public DistanceFieldRenderPass(RenderPassEvent renderPassEvent, string tagName)
            {
                this.renderPassEvent = renderPassEvent;
                profilingSampler = new ProfilingSampler(tagName);
            }
            
            enum PassIndex
            {
                DistanceMask,
                Combine
            }
            
            public DistanceField settings;
            private CommandBuffer cmd;
            private BlurRenderPass m_BlurPass = new BlurRenderPass();
            
            private static Material m_Material;
            private static Material material
            {
                get
                {
                    if (!m_Material)
                    {
                        var shader = Shader.Find("RoxamiRP/Utils/DistanceField");
                        if (!shader)
                        {
                            return null;
                        }
                        
                        m_Material = CoreUtils.CreateEngineMaterial(shader);
                    }
                    
                    return m_Material;
                }
            }

            private const string distanceMaskRtName = "_PostBlurMaskTexture";
            private static readonly int distanceMaskRtID = Shader.PropertyToID(distanceMaskRtName);
            private RTHandle distanceMaskRT;
            
            private const string blurRtName = "_PostBlurInputTexture";
            private static readonly int blurRtID = Shader.PropertyToID(blurRtName);
            private RTHandle blurRT;
            
            private static readonly int cameraColorTextureID = Shader.PropertyToID("_CameraColorTexture");
            
            private static readonly int distanceFieldParamsID = Shader.PropertyToID("_DistanceFieldParams");

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (settings == null || !material) return;
                
                cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    cmd.SetGlobalVector(distanceFieldParamsID, new Vector4(
                        settings.distance.value, 
                        settings.distanceRange.value, 
                        settings.distanceRange.value + settings.near.value, 
                        settings.distanceRange.value + settings.far.value));
                    
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    int width = desc.width;
                    int height = desc.height;
                    
                    RenderTextureDescriptor maskDesc = new RenderTextureDescriptor(width, height)
                    {
                        depthBufferBits = 0,
                        colorFormat = RenderTextureFormat.RFloat
                    };
                    
                    RenderingUtils.ReAllocateIfNeeded(ref distanceMaskRT, maskDesc, name: distanceMaskRtName);
                    cmd.SetRenderTarget(distanceMaskRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    RoxamiCommonUtils.DrawFullScreenTriangle(cmd, material, (int)PassIndex.DistanceMask);

                    
                    RenderTextureDescriptor blurDesc = desc;
                    blurDesc.width = Mathf.Max(2, blurDesc.width >> settings.blurSettings.value.downSample);
                    blurDesc.height = Mathf.Max(2, blurDesc.height >> settings.blurSettings.value.downSample);
                    blurDesc.depthBufferBits = 0;

                    var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    RenderingUtils.ReAllocateIfNeeded(ref blurRT, blurDesc, name:blurRtName);
                    m_BlurPass.Setup(cmd, cameraColorTarget, blurRT, settings.blurSettings.value, blurDesc, BlurMaterialMode.Mask);
                    m_BlurPass.SetMaskBlurTexture(distanceMaskRT);
                    m_BlurPass.Execute(context, ref renderingData);

                    cmd.SetGlobalTexture(blurRtID, blurRT);
                    cmd.SetGlobalTexture(cameraColorTextureID, cameraColorTarget);
                    Blit(cmd, ref renderingData, material, (int)PassIndex.Combine);
                }
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                distanceMaskRT?.Release();
                m_BlurPass?.Dispose();
            }
        }
    }
}