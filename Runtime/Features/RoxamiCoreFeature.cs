using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class RoxamiCoreFeature : ScriptableRendererFeature
    {
        GlobalFogRenderPass globalFogRenderPass;
        
        public override void Create()
        {
            globalFogRenderPass = new GlobalFogRenderPass(RenderPassEvent.BeforeRenderingSkybox);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (globalFogRenderPass != null)
            {
                renderer.EnqueuePass(globalFogRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            globalFogRenderPass?.Dispose();
        }
    }
}