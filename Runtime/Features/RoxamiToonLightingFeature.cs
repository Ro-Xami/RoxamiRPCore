using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace RoxamiRPCore
{
    [Serializable]
    public class RoxamiToonLightingSettings
    {
        public Material deferredMaterial;
        
        [Header("Rendering Debug")]
        public bool isRenderingDebug;
        public RoxamiRenderingDebugOutput renderingOutput = RoxamiRenderingDebugOutput.None;
    }
    
    [Serializable]
    public enum RoxamiRenderingDebugOutput
    {
        None,
        Albedo,
        Normal,
        Metallic,
        Smoothness,
        Occlusion,
        MSA,
    }

    public class RoxamiToonLightingFeature : ScriptableRendererFeature
    {
        private static RoxamiToonLightingFeature m_Instance;
        public static RoxamiToonLightingFeature Instance
        {
            get
            {
                if (!m_Instance)
                {
                    m_Instance = CreateInstance<RoxamiToonLightingFeature>();
                    m_Instance.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_Instance;
            }
        }
        
        public RoxamiToonLightingSettings settings;
        
        private ToonLightingPass toonLightingPass;

        public override void Create()
        {
            toonLightingPass = new ToonLightingPass(settings, RenderPassEvent.BeforeRenderingDeferredLights + 1);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(toonLightingPass);
        }

        protected override void Dispose(bool disposing)
        {
            toonLightingPass?.Dispose();
        }

        public void SetRenderingDebugMode(bool active, RoxamiRenderingDebugOutput output)
        {
            if (settings == null) return;

            settings.isRenderingDebug = active;
            settings.renderingOutput = output;
        }
    }
    
    public class ToonLightingPass : ScriptableRenderPass
    {
        public ToonLightingPass(RoxamiToonLightingSettings settings, RenderPassEvent evt)
        {
            renderPassEvent = evt;
            m_Settings = settings;
            m_DeferredToonMaterial = m_Settings.deferredMaterial;
            
            profilingSampler = new ProfilingSampler(bufferName);

            InitDeferredPlusShadingMaterial();
        }
        
        private readonly RoxamiToonLightingSettings m_Settings;

        private Material m_DeferredToonMaterial;
        private Material DeferredToonMaterial
        {
            get
            {
                if (!m_DeferredToonMaterial)
                {
                    m_DeferredToonMaterial = CoreUtils.CreateEngineMaterial(RoxamiShaderConst.deferredToonShaderName);
                }
                
                return m_DeferredToonMaterial;
            }
        }

        private static readonly string[] renderingDebugKeywords = new string[]
        {
            "_RoDebug_None",
            "_RoDebug_Albedo",
            "_RoDebug_Normal",
            "_RoDebug_Metallic",
            "_RoDebug_Smoothness",
            "_RoDebug_Occlusion",
            "_RoDebug_MSA"
        };

        private const string bufferName = "RoxamiToonLighting";
        private CommandBuffer cmd;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                DeferredLighting(context, ref renderingData);

                // DrawConvolutionOutline();
                
                // DrawClusteredDebug();
                //
                DrawRenderingDebug(renderingData);
            }
            ExecuteCommandBuffer(context, cmd);
 
        }

        public void Dispose()
        {
            // CoreUtils.Destroy(m_DeferredToonMaterial);
        }
        
        private void DeferredLighting(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecuteCommandBuffer(context, cmd);
               
            RoxamiCommonUtils.SetupScreenToWorldMatrixConstants(cmd, ref renderingData);
            cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ToonLit);
        }

        private void DrawConvolutionOutline()
        {
            // cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ConvolutionOutline);
        }

        private void DrawClusteredDebug()
        {
#if UNITY_EDITOR
            // if (m_Settings.isClusteredDebug)
            // {
            //     cmd.SetGlobalFloat(clusteredDebugIndexZID, clusteredSettings.clusteredDebugIndexZ);
            //     cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ClusteredDebug);
            // }
#endif
        }

        private void DrawRenderingDebug(RenderingData renderingData)
        {
#if UNITY_EDITOR
            var cameraType = renderingData.cameraData.camera.cameraType;
            
            if (!m_Settings.isRenderingDebug ||
                m_Settings.renderingOutput == RoxamiRenderingDebugOutput.None ||
                (cameraType != CameraType.Game && cameraType != CameraType.SceneView))
                return;

            cmd.ClearRenderTarget(false, true, Color.clear);

            switch (m_Settings.renderingOutput)
            {
                case RoxamiRenderingDebugOutput.None:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.None);
                    break;
                case RoxamiRenderingDebugOutput.Albedo:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.Albedo);
                    break;
                case RoxamiRenderingDebugOutput.Normal:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.Normal);
                    break;
                case RoxamiRenderingDebugOutput.Metallic:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.Metallic);
                    break;
                case RoxamiRenderingDebugOutput.Smoothness:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.Smoothness);
                    break;
                case RoxamiRenderingDebugOutput.Occlusion:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.Occlusion);
                    break;
                case RoxamiRenderingDebugOutput.MSA:
                    EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput.MSA);
                    break;
            }
            
            cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.RenderingDebug);
#endif
        }

        void EnableRenderingDebugKeyword(RoxamiRenderingDebugOutput output)
        {
            foreach (var keyword in renderingDebugKeywords)
            {
                cmd.DisableShaderKeyword(keyword);
            }
            cmd.EnableShaderKeyword(renderingDebugKeywords[(int)output]);
        }
        
        void InitDeferredPlusShadingMaterial()
        {
            if (DeferredToonMaterial == null)
                return;
            
            DeferredToonMaterial.SetFloat(DeferredLights.ShaderConstants._LitStencilRef, (float)StencilUsage.MaterialLit);
            DeferredToonMaterial.SetFloat(DeferredLights.ShaderConstants._LitStencilReadMask, (float)StencilUsage.MaterialMask);
            DeferredToonMaterial.SetFloat(DeferredLights.ShaderConstants._SimpleLitStencilRef, (float)StencilUsage.MaterialSimpleLit);
            DeferredToonMaterial.SetFloat(DeferredLights.ShaderConstants._SimpleLitStencilReadMask, (float)StencilUsage.MaterialMask);
        }

        void ExecuteCommandBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer)
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
    }
}