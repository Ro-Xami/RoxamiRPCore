using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            
            settings = m_Settings;
            cs = settings.computeShader;
            clusterCullingKernel = cs.FindKernel(cullingKernelName);

            clusterCountX = settings.threadGroupX * numThreadsX;
            clusterCountY = settings.threadGroupY * numThreadsY;
            var clusterLightCountBufferCount = clusterCountX * clusterCountY;
            var clusterLightIndexBufferCount = clusterLightCountBufferCount * settings.maxClusterLightIndex;
            clusterLightCountBuffer = new ComputeBuffer(clusterLightCountBufferCount, sizeof(int));
            clusterLightIndexBuffer = new ComputeBuffer(clusterLightIndexBufferCount, sizeof(int));
        }

        private readonly ClusteredLightingSettings settings;
        
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
            if (settings == null || !settings.isActive || !cs || clusterCullingKernel < 0 || !DeferredToonMaterial)
                return false;
            
            return true;
        }

        public override void Execute(ScriptableRenderContext context, CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            cmd = commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalFloat(RoxamiShaderConst.roxamiAdditionalLightsCountID, renderingData.lightData.additionalLightsCount);
                cmd.SetGlobalVector(clusterCountID, new Vector4(clusterCountX, clusterCountY));
                cmd.SetGlobalInt(maxClusterLightIndexID, settings.maxClusterLightIndex);
                
                cmd.SetGlobalVector(cameraRightDirID, new Vector4(
                    renderingData.cameraData.camera.transform.right.x,
                    renderingData.cameraData.camera.transform.right.y,
                    renderingData.cameraData.camera.transform.right.z,
                    renderingData.cameraData.camera.aspect));
                
                cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightCountBufferID, clusterLightCountBuffer);
                cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightIndexBufferID, clusterLightIndexBuffer);
                cmd.DispatchCompute(cs, clusterCullingKernel, settings.threadGroupX, settings.threadGroupY, 1);
                
                cmd.SetGlobalBuffer(clusterLightCountBufferID, clusterLightCountBuffer);
                cmd.SetGlobalBuffer(clusterLightIndexBufferID, clusterLightIndexBuffer);
                ExecuteCommandBuffer(context, cmd);
               
                RoxamiCommonUtils.SetupMatrixConstants(cmd, ref renderingData);
                cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ToonLit);
#if UNITY_EDITOR
                if (settings.isDebug)
                {
                    cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ClusteredDebug);
                }
#endif
            }
            ExecuteCommandBuffer(context, cmd);
 
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_DeferredToonMaterial);
        }
    }
}