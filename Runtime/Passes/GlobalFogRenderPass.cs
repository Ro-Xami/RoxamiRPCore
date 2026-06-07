using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class GlobalFogRenderPass : ScriptableRenderPass
    {
        public GlobalFogRenderPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            profilingSampler = new ProfilingSampler("RoxamiGlobalFog");
        }
        
        private static Material m_Material;

        private static Material material
        {
            get
            {
                if (!m_Material)
                {
                    var shader = Shader.Find(RoxamiShaderConst.globalFogShaderName);
                    if (!shader) return null;
                    
                    m_Material = CoreUtils.CreateEngineMaterial(shader);
                }
                return m_Material;
            }
        }
        
        private static readonly int fogParamsID = Shader.PropertyToID("_RoxamiGlobalFogParams");
        private static readonly int fogColorID = Shader.PropertyToID("_RoxamiGlobalFogColor");

        private CommandBuffer cmd;
        
        private RoxamiGlobalFog volume;

        public void UpdateVolume(RoxamiGlobalFog volume)
        {
            this.volume = volume;
        }
        
        public static void DisableKeyword(CommandBuffer commandBuffer)
        {
            foreach (var m_Keyword in RoxamiShaderConst.globalFogKeywords)
            {
                commandBuffer.DisableShaderKeyword(m_Keyword);
            }
            commandBuffer.EnableShaderKeyword(RoxamiShaderConst.globalFogKeywords[(int)RoxamiFogMode.None]);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (volume == null || !material)
                return;
            
            cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                RenderFog();
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        void RenderFog()
        {
            EnableFogKeyword(RoxamiShaderConst.globalFogKeywords[(int)volume.fogMode.value]);
            
            UpdateFogShaderData(volume);

            RoxamiCommonUtils.DrawFullScreenTriangle(cmd, material, 0);
        }

        void UpdateFogShaderData(RoxamiGlobalFog settings)
        {
            cmd.SetGlobalVector(fogParamsID, new Vector4(
                settings.fogStart.value, settings.fogEnd.value, settings.density.value));
            
            cmd.SetGlobalColor(fogColorID, settings.fogColor.value);
        }

        void EnableFogKeyword(string keyword)
        {
            foreach (var m_Keyword in RoxamiShaderConst.globalFogKeywords)
            {
                cmd.DisableShaderKeyword(m_Keyword);
            }
            cmd.EnableShaderKeyword(keyword);
        }
    }
}