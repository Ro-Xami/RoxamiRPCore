using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class ProximityColorModifierFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        ProximityColorModifierPass m_Pass;

        public override void Create()
        {
            m_Pass = new ProximityColorModifierPass(renderPassEvent, this.name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;

            var settings = VolumeManager.instance.stack.GetComponent<ProximityColorModifier>();
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

        class ProximityColorModifierPass : ScriptableRenderPass
        {
            public ProximityColorModifierPass(RenderPassEvent renderPassEvent, string tagName)
            {
                this.renderPassEvent = renderPassEvent;
                profilingSampler = new ProfilingSampler(tagName);
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public ProximityColorModifier settings;
            private CommandBuffer cmd;

            private static Material m_Material;
            private static Material material
            {
                get
                {
                    if (!m_Material)
                    {
                        var shader = Shader.Find("RoxamiRP/Utils/ProximityColorModifier");
                        if (!shader)
                        {
                            return null;
                        }

                        m_Material = CoreUtils.CreateEngineMaterial(shader);
                    }

                    return m_Material;
                }
            }

            private static readonly int modifyColorID = Shader.PropertyToID("_PostProximityColor");
            private static readonly int proximityDistanceID = Shader.PropertyToID("_PostProximityDistance");
            private static readonly int softnessID = Shader.PropertyToID("_PostProximitySoftness");

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (settings == null || !material) return;

                cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    cmd.SetGlobalColor(modifyColorID, settings.modifyColor.value);
                    cmd.SetGlobalFloat(proximityDistanceID, settings.proximityDistance.value);
                    cmd.SetGlobalFloat(softnessID, settings.softness.value);

                    RoxamiCommonUtils.DrawFullScreenTriangle(cmd, material, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(m_Material);
            }
        }
    }
}
