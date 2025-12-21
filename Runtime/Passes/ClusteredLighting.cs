using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public class ClusteredLightingPass : ScriptableRenderPass
    {
        public ClusteredLightingPass(ClusteredLightingSettings m_Settings)
        {
            if (m_Settings == null || !m_Settings.computeShader || m_Settings.computeShader.FindKernel(cullingKernelName) < 0)
                return;
            
            renderPassEvent = ClusteredLightingSettings.renderPassEvent;
            profilingSampler = new ProfilingSampler(bufferName);
            
            cs = m_Settings.computeShader;
            cullingKernel = cs.FindKernel(cullingKernelName);
            
            threadGroupX = m_Settings.threadGroupX;
            threadGroupY = m_Settings.threadGroupY;
            threadGroupZ = m_Settings.threadGroupZ;
            threadCountX = threadGroupX * numThreadsX;
            threadCountY = threadGroupY * numThreadsY;
            threadCountZ = threadGroupZ * numThreadsZ;

            int threadCount = threadCountX * threadCountY * threadCountZ;
            clusterLightCountBuffer = new ComputeBuffer(threadCount, sizeof(int));
            clusterLightIndexBuffer = new ComputeBuffer(threadCount * maxLightCount, sizeof(int));
        }
        
        private const string cullingKernelName = "ClusteredBoxGetLightCulling";
        private const int maxLightCount = 10;
        private const int numThreadsX = 8;
        private const int numThreadsY = 8;
        private const int numThreadsZ = 1;
        
        private static readonly int clusterLightCountBufferID = Shader.PropertyToID("_ClusterLightCountBuffer");
        private static readonly int clusterLightIndexBufferID = Shader.PropertyToID("_ClusterLightIndexBuffer");
        private static readonly int threadGroupID = Shader.PropertyToID("_ClusteredLightingThreadCount");
        private static readonly int cameraPlanesID = Shader.PropertyToID("_CameraPlanes");

        private readonly ComputeShader cs;
        private readonly int cullingKernel;
        private readonly int threadGroupX, threadGroupY, threadGroupZ;
        private readonly int threadCountX, threadCountY, threadCountZ;
        private readonly ComputeBuffer clusterLightCountBuffer;
        private readonly ComputeBuffer clusterLightIndexBuffer;
        
        enum RoxamiToonDeferredPassInput
        {
            ToonLit,
        }
        
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
        
        static readonly int roxamiAdditionalLightsCountID = Shader.PropertyToID("_RoxamiAdditionalLightsCount");

        private const string bufferName = "ClusterLighting";
        private CommandBuffer cmd;

        public override void Configure(CommandBuffer commandBuffer, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!cs || cullingKernel < 0) return;

            cmd = CommandBufferPool.Get(bufferName);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(renderingData.cameraData.camera);
                var cameraPlanes = new Vector4[planes.Length];
                for (int i = 0; i < cameraPlanes.Length; i++)
                {
                    cameraPlanes[i] = new Vector4(
                        planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance * planes[i].distance);
                }
                
                cmd.SetComputeVectorArrayParam(cs, cameraPlanesID, cameraPlanes);
                cmd.SetComputeBufferParam(cs, cullingKernel, clusterLightCountBufferID, clusterLightCountBuffer);
                cmd.SetComputeBufferParam(cs, cullingKernel, clusterLightIndexBufferID, clusterLightIndexBuffer);
                cmd.SetComputeVectorParam(cs, threadGroupID, new Vector4(threadCountX, threadCountY, threadCountZ));
                
                cmd.DispatchCompute(cs, cullingKernel, threadGroupX, threadGroupY, threadGroupZ);
                
                cmd.SetGlobalFloat(roxamiAdditionalLightsCountID, renderingData.lightData.additionalLightsCount);
                cmd.DrawMesh(RoxamiCommonUtils.FullScreenMesh, Matrix4x4.identity, DeferredToonMaterial, 0, (int)RoxamiToonDeferredPassInput.ToonLit);
                
                //renderingData.cameraData.re
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer commandBuffer)
        {

        }

        public void Dispose()
        {
            
        }
    }
}