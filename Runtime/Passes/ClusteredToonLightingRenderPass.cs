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
        public bool isActive;
        public bool isDebug;
        public Material deferredMaterial;
        public ComputeShader computeShader;
        [Range(1, 99)] public int maxClusterLightIndex = 10;
        [Range(1, 5)] public int threadGroupX = 10;
        [Range(1, 5)] public int threadGroupY = 10;
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
            var clusterLightCountBufferCount = clusterCountX * clusterCountY;
            var clusterLightIndexBufferCount = clusterLightCountBufferCount * clusteredSettings.maxClusterLightIndex;
            clusterLightCountBuffer = new ComputeBuffer(clusterLightCountBufferCount, sizeof(int));
            clusterLightIndexBuffer = new ComputeBuffer(clusterLightIndexBufferCount, sizeof(int));
        }

        //ClusteredLights
        private readonly ClusteredLightingSettings clusteredSettings;
        
        private const string cullingKernelName = "ClusteredLights";
        private const int numThreadsX = 8;
        private const int numThreadsY = 8;
        
        private static readonly int clusterLightCountBufferID = Shader.PropertyToID("_ClusterLightCountBuffer");
        private static readonly int clusterLightIndexBufferID = Shader.PropertyToID("_ClusterLightIndexBuffer");
        private static readonly int maxClusterLightIndexID = Shader.PropertyToID("_MaxClusterLightIndex");
        private static readonly int clusterCountID = Shader.PropertyToID("_ClusterCount");
        private static readonly int cameraRightDirID = Shader.PropertyToID("_CameraRightDir");

        private readonly ComputeShader cs;
        private readonly int clusterCullingKernel;
        private readonly int clusterCountX, clusterCountY;
        private readonly ComputeBuffer clusterLightCountBuffer;
        private readonly ComputeBuffer clusterLightIndexBuffer;

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
                cmd.SetGlobalVector(clusterCountID, new Vector4(clusterCountX, clusterCountY));
                cmd.SetGlobalInt(maxClusterLightIndexID, clusteredSettings.maxClusterLightIndex);
                
                ClusteredLights(renderingData);

                DeferredLighting(context, ref renderingData);

                DrawConvolutionOutline();

                ClearStencil();
                
                DrawDebug();
            }
            ExecuteCommandBuffer(context, cmd);
 
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_DeferredToonMaterial);
        }

        private void ClusteredLights(RenderingData renderingData)
        {
            cmd.SetGlobalVector(cameraRightDirID, new Vector4(
                renderingData.cameraData.camera.transform.right.x,
                renderingData.cameraData.camera.transform.right.y,
                renderingData.cameraData.camera.transform.right.z,
                renderingData.cameraData.camera.aspect));
                
            cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightCountBufferID, clusterLightCountBuffer);
            cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightIndexBufferID, clusterLightIndexBuffer);
            cmd.DispatchCompute(cs, clusterCullingKernel, clusteredSettings.threadGroupX, clusteredSettings.threadGroupY, 1);
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

        private void DrawDebug()
        {
#if UNITY_EDITOR
            if (clusteredSettings.isDebug)
            {
                cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ClusteredDebug);
            }
#endif
        }
        
    }
}