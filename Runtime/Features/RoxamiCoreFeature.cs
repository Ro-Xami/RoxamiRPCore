using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class RoxamiCoreFeature : ScriptableRendererFeature
    {
        [SerializeField] ActorForwardSettings actorForwardSettings;
        
        ActorForwardRenderPass actorForwardRenderPass;
        
        GlobalFogRenderPass globalFogRenderPass;
        
        public override void Create()
        {
            actorForwardRenderPass =
                new ActorForwardRenderPass(actorForwardSettings, RenderPassEvent.AfterRenderingOpaques);
            
            globalFogRenderPass = new GlobalFogRenderPass(RenderPassEvent.BeforeRenderingSkybox);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (actorForwardRenderPass != null)
            {
                renderer.EnqueuePass(actorForwardRenderPass);
            }
            
            if (globalFogRenderPass != null)
            {
                renderer.EnqueuePass(globalFogRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            actorForwardRenderPass?.Dispose();
            globalFogRenderPass?.Dispose();
        }
    }
}