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
        public ComputeShader computeShader;
        [Min(5)] public int maxClusterLightIndex = 10;
        [Range(1, 5)] public int threadGroupX = 10;
        [Range(1, 5)] public int threadGroupY = 10;
    }
    
    public class ClusteredLightingPass : RoxamiDeferredLights
    {
        public ClusteredLightingPass(ClusteredLightingSettings m_Settings)
        {
            if (m_Settings == null || !m_Settings.computeShader || m_Settings.computeShader.FindKernel(cullingKernelName) < 0)
                return;

            profilingSampler = new ProfilingSampler(bufferName);
            
            cs = m_Settings.computeShader;
            clusterCullingKernel = cs.FindKernel(cullingKernelName);
            maxClusterLightIndex = m_Settings.maxClusterLightIndex;
            isDebugClusterLights = m_Settings.isDebug;
            
            threadGroupX = m_Settings.threadGroupX;
            threadGroupY = m_Settings.threadGroupY;
            threadCountX = threadGroupX * numThreadsX;
            threadCountY = threadGroupY * numThreadsY;

            var clusterLightCountBufferCount = threadCountX * threadCountY;
            var clusterLightIndexBufferCount = clusterLightCountBufferCount * maxClusterLightIndex;
            clusterLightCountBuffer = new ComputeBuffer(clusterLightCountBufferCount, sizeof(int));
            clusterLightIndexBuffer = new ComputeBuffer(clusterLightIndexBufferCount, sizeof(int));
        }
        
        private const string cullingKernelName = "ClusteredBoxGetLightCulling";
        private const int numThreadsX = 8;
        private const int numThreadsY = 8;
        
        private static readonly int clusterLightCountBufferID = Shader.PropertyToID("_ClusterLightCountBuffer");
        private static readonly int clusterLightIndexBufferID = Shader.PropertyToID("_ClusterLightIndexBuffer");
        private static readonly int maxClusterLightIndexID = Shader.PropertyToID("_MaxClusterLightIndex");
        private static readonly int clusterCountID = Shader.PropertyToID("_ClusterCount");

        private readonly ComputeShader cs;
        private readonly int clusterCullingKernel;
        private readonly int maxClusterLightIndex;
        private readonly int threadGroupX, threadGroupY;
        private readonly int threadCountX, threadCountY;
        private readonly ComputeBuffer clusterLightCountBuffer;
        private readonly ComputeBuffer clusterLightIndexBuffer;

        private readonly bool isDebugClusterLights = false;

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
            return true;
        }

        public override void Execute(ScriptableRenderContext context, CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            if (!cs || clusterCullingKernel < 0) return;

            cmd = commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalFloat(RoxamiShaderConst.roxamiAdditionalLightsCountID, renderingData.lightData.additionalLightsCount);
                cmd.SetGlobalVector(clusterCountID, new Vector4(threadCountX, threadCountY));
                cmd.SetGlobalInt(maxClusterLightIndexID, maxClusterLightIndex);
                
                cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightCountBufferID, clusterLightCountBuffer);
                cmd.SetComputeBufferParam(cs, clusterCullingKernel, clusterLightIndexBufferID, clusterLightIndexBuffer);

                cmd.DispatchCompute(cs, clusterCullingKernel, threadGroupX, threadGroupY, 1);
                
                cmd.SetGlobalBuffer(clusterLightCountBufferID, clusterLightCountBuffer);
                cmd.SetGlobalBuffer(clusterLightIndexBufferID, clusterLightIndexBuffer);
                ExecuteCommandBuffer(context, cmd);
               
                RoxamiCommonUtils.SetupMatrixConstants(cmd, ref renderingData);

                cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ToonLit);
#if UNITY_EDITOR
                if (isDebugClusterLights)
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