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
        
        const string bufferName = "GlobalFogPass";
        private CommandBuffer cmd;
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
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
            var settings = VolumeManager.instance.stack.GetComponent<RoxamiGlobalFog>();

            UpdateFogShaderData(settings);
            
            if (!settings.IsActive() || !material)
            {
                EnableFogKeyword(RoxamiShaderConst.globalFogKeywords[(int)RoxamiFogMode.None]);
                return;
            }

            EnableFogKeyword(RoxamiShaderConst.globalFogKeywords[(int)settings.fogMode.value]);

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