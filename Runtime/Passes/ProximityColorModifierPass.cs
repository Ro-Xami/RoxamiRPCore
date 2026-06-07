using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class ProximityColorModifierPass : ScriptableRenderPass
    {
        public ProximityColorModifierPass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            profilingSampler = new ProfilingSampler("ProximityColorModifier");
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        private ProximityColorModifier volume;
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

        public void UpdateVolume(ProximityColorModifier volume)
        {
            this.volume = volume;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (volume == null || !material) return;

            cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalColor(modifyColorID, volume.modifyColor.value);
                cmd.SetGlobalFloat(proximityDistanceID, volume.proximityDistance.value);
                cmd.SetGlobalFloat(softnessID, volume.softness.value);

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
