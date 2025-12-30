using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    public static class RoxamiCommonUtils
    {
        private static Mesh m_FullscreenMesh;
        public static Mesh FullScreenMesh
        {
            get
            {
                if (!m_FullscreenMesh)
                {
                    m_FullscreenMesh = CreateFullscreenMesh();
                }
                return m_FullscreenMesh;
            }
        }
        
        //From urp DeferredLights
        static Mesh CreateFullscreenMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // Simple full-screen triangle.
            Vector3[] positions =
            {
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -3.0f, 0.0f),
                new Vector3(3.0f,  1.0f, 0.0f)
            };

            int[] indices = { 0, 1, 2 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }
        
        private static readonly Matrix4x4[] m_ScreenToWorld = new Matrix4x4[2];
        
        //From Urp DeferredLights
        public static void SetupScreenToWorldMatrixConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            
            var RenderWidth = cameraData.camera.allowDynamicResolution ? 
                Mathf.CeilToInt(ScalableBufferManager.widthScaleFactor * renderingData.cameraData.cameraTargetDescriptor.width) : 
                renderingData.cameraData.cameraTargetDescriptor.width;
            var RenderHeight = cameraData.camera.allowDynamicResolution ? 
                Mathf.CeilToInt(ScalableBufferManager.heightScaleFactor * renderingData.cameraData.cameraTargetDescriptor.height) : 
                renderingData.cameraData.cameraTargetDescriptor.height;
            
            var IsOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
            
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            Matrix4x4[] screenToWorld = m_ScreenToWorld; // deferred shaders expects 2 elements

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, false);

                // xy coordinates in range [-1; 1] go to pixel coordinates.
                Matrix4x4 toScreen = new Matrix4x4(
                    new Vector4(0.5f * RenderWidth, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.5f * RenderHeight, 0.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                    new Vector4(0.5f * RenderWidth, 0.5f * RenderHeight, 0.0f, 1.0f)
                );

                Matrix4x4 zScaleBias = Matrix4x4.identity;
                if (IsOpenGL)
                {
                    // We need to manunally adjust z in NDC space from [-1; 1] to [0; 1] (storage in depth texture).
                    zScaleBias = new Matrix4x4(
                        new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.5f, 1.0f)
                    );
                }

                screenToWorld[eyeIndex] = Matrix4x4.Inverse(toScreen * zScaleBias * gpuProj * view);
            }

            cmd.SetGlobalMatrixArray(RoxamiShaderConst.screenToWorldID, screenToWorld);
        }

        public static void DrawFullScreenTriangle(CommandBuffer cmd, Material mat, int passIndex = 0)
        {
            cmd.DrawProcedural(
                Matrix4x4.identity, mat, passIndex,
                MeshTopology.Triangles, 3
            );
        }

    }
    
    public enum RoxamiToonDeferredPassInput
    {
        ToonLit,
        ConvolutionOutline,
        ClusteredDebug
    }
    
    public static class RoxamiShaderConst
    {
        public const string deferredToonShaderName = "RoxamiRP/Core/ToonDeferred";
        public const string convolutionOutlineShaderName = "RoxamiRP/Core/ConvolutionOutline";

        public const string convolutionOutlineKeyword = "ConvolutionOutline_ON";
        
        public static readonly int screenToWorldID = Shader.PropertyToID("_ScreenToWorld");
        public static readonly int roxamiAdditionalLightsCountID = Shader.PropertyToID("_RoxamiAdditionalLightsCount");
        public static readonly int convolutionOutlineTextureID = Shader.PropertyToID("_ConvolutionOutlineTexture");
    }
}