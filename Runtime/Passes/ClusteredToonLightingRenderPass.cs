using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace RoxamiRPCore
{
    [Serializable]
    public class ClusteredLightingSettings
    {
        [Header("Clustered Lighting")]
        public bool isActive;
        public bool isClusteredDebug;
        [Range(0, 0.999f)] public float clusteredDebugIndexZ = 0;
        public Material deferredMaterial;
        public ComputeShader computeShader;
        [Range(1, 99)] public int maxClusterLightIndex = 10;
        [Range(1, 5)] public int threadGroupX = 10;
        [Range(1, 5)] public int threadGroupY = 10;
        [Range(1, 5)] public int threadGroupZ = 10;
        
        [Header("Rendering Debug")]
        public bool isRenderingDebug;
        public RoxamiRenderingDebugOutput renderingOutput = RoxamiRenderingDebugOutput.None;
    }
    
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
    
    public class ClusteredLightingPass : RoxamiDeferredLights
    {
        public ClusteredLightingPass(ClusteredLightingSettings m_Settings)
        {
            if (m_Settings == null || !m_Settings.computeShader || m_Settings.computeShader.FindKernel(cullingKernelName) < 0)
                return;
            
            m_DeferredToonMaterial = m_Settings.deferredMaterial;
            
            profilingSampler = new ProfilingSampler(bufferName);
            
            clusteredSettings = m_Settings;
            cs = clusteredSettings.computeShader;
            clusterCullingKernel = cs.FindKernel(cullingKernelName);

            clusterCountX = clusteredSettings.threadGroupX * numThreadsX;
            clusterCountY = clusteredSettings.threadGroupY * numThreadsY;
            clusterCountZ = clusteredSettings.threadGroupZ * numThreadsZ;
            var clusterLightCountBufferCount = clusterCountX * clusterCountY * clusterCountZ;
            var clusterLightIndexBufferCount = clusterLightCountBufferCount * clusteredSettings.maxClusterLightIndex;
            clusterLightCountBuffer = new ComputeBuffer(clusterLightCountBufferCount, sizeof(int));
            clusterLightIndexBuffer = new ComputeBuffer(clusterLightIndexBufferCount, sizeof(int));
            
            Shader.SetGlobalInt(maxClusterCountID, clusterCountX * clusterCountY * clusterCountZ);
        }

        //ClusteredLights
        private readonly ClusteredLightingSettings clusteredSettings;
        
        private const string cullingKernelName = "ClusteredLights";
        private const int numThreadsX = 8;
        private const int numThreadsY = 8;
        private const int numThreadsZ = 1;
        
        private static readonly int clusterLightCountBufferID = Shader.PropertyToID("_ClusterLightCountBuffer");
        private static readonly int clusterLightIndexBufferID = Shader.PropertyToID("_ClusterLightIndexBuffer");
        private static readonly int maxClusterLightIndexID = Shader.PropertyToID("_MaxClusterLightIndex");
        private static readonly int clusterCountID = Shader.PropertyToID("_ClusterCount");
        private static readonly int maxClusterCountID = Shader.PropertyToID("_MaxClusterCount");
        private static readonly int cameraViewportPointsWsID = Shader.PropertyToID("_CameraViewportPointsWS");
        private static readonly int clusteredDebugIndexZID = Shader.PropertyToID("_ClusteredDebugIndexZ");

        private readonly ComputeShader cs;
        private readonly int clusterCullingKernel;
        private readonly int clusterCountX, clusterCountY, clusterCountZ;
        private readonly ComputeBuffer clusterLightCountBuffer;
        private readonly ComputeBuffer clusterLightIndexBuffer;

        //    7 -------- 6
        //   /|         /|
        //  / |        / |
        // 3 -------- 2  |
        // |  |       |  |
        // |  4 ------|--5
        // | /        | /
        // |/         |/
        // 0 -------- 1
        private readonly Vector4[] cameraViewportPointsWS = new Vector4[8];

        private static Material m_DeferredToonMaterial;
        private static Material DeferredToonMaterial
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

        private const string bufferName = "ClusterLighting";
        private readonly ProfilingSampler profilingSampler;
        private CommandBuffer cmd;

        public override bool NeedToExecute()
        {
            if (clusteredSettings == null || !clusteredSettings.isActive || !cs || clusterCullingKernel < 0 || !DeferredToonMaterial)
                return false;
            
            return true;
        }

        public override void InitializeAdditionalLightsData(ref RenderingData renderingData, AdditionalLightsShadowCasterPass shadowCasterPass) { }

        public override void Execute(ScriptableRenderContext context, CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            cmd = commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalFloat(RoxamiShaderConst.roxamiAdditionalLightsCountID, renderingData.lightData.additionalLightsCount);
                cmd.SetGlobalVector(clusterCountID, new Vector4(clusterCountX, clusterCountY, clusterCountZ));
                cmd.SetGlobalInt(maxClusterLightIndexID, clusteredSettings.maxClusterLightIndex);
                
                ClusteredLights(renderingData);

                DeferredLighting(context, ref renderingData);

                DrawConvolutionOutline();

                ClearStencil();
                
                DrawClusteredDebug();

                DrawRenderingDebug(renderingData);
            }
            ExecuteCommandBuffer(context, cmd);
 
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_DeferredToonMaterial);
        }

        private void ClusteredLights(RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            
            //获得摄像机视锥体的八个角点
            float near = camera.nearClipPlane;
            float far  = camera.farClipPlane;
            
            // Near
            cameraViewportPointsWS[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, near)); // Left Bottom
            cameraViewportPointsWS[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, near)); // Right Bottom
            cameraViewportPointsWS[2] = camera.ViewportToWorldPoint(new Vector3(1, 1, near)); // Right Top
            cameraViewportPointsWS[3] = camera.ViewportToWorldPoint(new Vector3(0, 1, near)); // Left Top

            // Far
            cameraViewportPointsWS[4] = camera.ViewportToWorldPoint(new Vector3(0, 0, far)); // Left Bottom
            cameraViewportPointsWS[5] = camera.ViewportToWorldPoint(new Vector3(1, 0, far)); // Right Bottom
            cameraViewportPointsWS[6] = camera.ViewportToWorldPoint(new Vector3(1, 1, far)); // Right Top
            cameraViewportPointsWS[7] = camera.ViewportToWorldPoint(new Vector3(0, 1, far)); // Left Top
            
            cmd.SetComputeVectorArrayParam(cs, cameraViewportPointsWsID, cameraViewportPointsWS);
            cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightCountBufferID, clusterLightCountBuffer);
            cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightIndexBufferID, clusterLightIndexBuffer);
            cmd.DispatchCompute(cs, clusterCullingKernel, clusteredSettings.threadGroupX, clusteredSettings.threadGroupY, clusteredSettings.threadGroupZ);
        }
        
        private void DeferredLighting(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cmd.SetGlobalBuffer(clusterLightCountBufferID, clusterLightCountBuffer);
            cmd.SetGlobalBuffer(clusterLightIndexBufferID, clusterLightIndexBuffer);
            ExecuteCommandBuffer(context, cmd);
               
            RoxamiCommonUtils.SetupScreenToWorldMatrixConstants(cmd, ref renderingData);
            cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ToonLit);
        }

        private void DrawConvolutionOutline()
        {
            cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ConvolutionOutline);
        }
        
        private void ClearStencil()
        {
            cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ClearStencil);
        }

        private void DrawClusteredDebug()
        {
#if UNITY_EDITOR
            if (clusteredSettings.isClusteredDebug)
            {
                cmd.SetGlobalFloat(clusteredDebugIndexZID, clusteredSettings.clusteredDebugIndexZ);
                cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ClusteredDebug);
            }
#endif
        }

        private void DrawRenderingDebug(RenderingData renderingData)
        {
#if UNITY_EDITOR
            var cameraType = renderingData.cameraData.camera.cameraType;
            
            if (!clusteredSettings.isRenderingDebug ||
                clusteredSettings.renderingOutput == RoxamiRenderingDebugOutput.None ||
                (cameraType != CameraType.Game && cameraType != CameraType.SceneView))
                return;

            cmd.ClearRenderTarget(false, true, Color.clear);

            switch (clusteredSettings.renderingOutput)
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
        
    }
}