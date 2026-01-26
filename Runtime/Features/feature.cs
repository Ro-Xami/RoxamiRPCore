using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class feature : ScriptableRendererFeature
    {
        public ActorForwardSettings settings;
        public RenderPassEvent evt;

        private ActorForwardRenderPass pass;
        public override void Create()
        {
            pass = new ActorForwardRenderPass(settings, evt);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
    }
}