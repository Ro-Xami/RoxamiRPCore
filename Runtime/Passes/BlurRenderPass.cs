using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoxamiRPCore
{
    [Serializable]
    public enum BlurType
    {
        Box,
        Gaussian,
    }
    
    [Serializable]
    public class BlurSettings
    {
        public BlurType blurType = BlurType.Box;
        [Range(0f, 10f)] public float blurRadios = 1.0f;
        [Range(1, 10)] public int iterations = 1;
        [Range(0, 10)] public int downSample = 0;
        
        public static BlurSettings Default = new BlurSettings()
        {
            blurType = BlurType.Box,
            blurRadios = 1,
            iterations = 1,
            downSample = 1,
        };
    }
    
    public class BlurRenderPass
    {
        enum BlurPassIndex
        {
            Box,
            Gaussian,
        }
        
        private BlurSettings m_Settings;

        private CommandBuffer m_CommandBuffer;
        
        private static readonly int targetID = Shader.PropertyToID("_PostBlurInputTexture");
        private static readonly int offsetID = Shader.PropertyToID("_PostBlurOffset");
        private RenderTextureDescriptor m_Descriptor;
        private RTHandle m_Target;
        
        private const int m_MaxSampleCount = 8;
        private const string blurSampleRTAName = "_BlurSampleA";
        private const string blurSampleRTBName = "_BlurSampleB";
        private readonly RTHandle[] blurSampleRTA = new RTHandle[m_MaxSampleCount];
        private readonly RTHandle[] blurSampleRTB = new RTHandle[m_MaxSampleCount];
        
        private static Material m_Material;
        private static Material material
        {
            get
            {
                if (!m_Material)
                {
                    var shader = Shader.Find(RoxamiShaderConst.blurShaderName);
                    if (!shader) return null;
                    
                    m_Material = CoreUtils.CreateEngineMaterial(shader);
                }
                return m_Material;
            }
        }

        public void Setup(CommandBuffer commandBuffer, RTHandle target, BlurSettings settings, RenderTextureDescriptor descriptor)
        {
            m_CommandBuffer = commandBuffer;
            m_Target = target;
            m_Settings = settings;
            m_Descriptor = descriptor;
        }

        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CommandBuffer == null || m_Target == null || m_Settings == null || !material) 
                return;

            switch (m_Settings.blurType)
            {
                case BlurType.Box:
                    BoxBlur(context, ref renderingData);
                    break;
                case BlurType.Gaussian:
                    GaussianBlur(context, ref renderingData);
                    break;
            }
            
            context.ExecuteCommandBuffer(m_CommandBuffer);
            m_CommandBuffer.Clear();
            
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);

            if (blurSampleRTA != null && blurSampleRTA.Length > 0)
            {
                foreach (var rt in blurSampleRTA)
                {
                    rt?.Release();
                }
            }
            
            if (blurSampleRTB != null && blurSampleRTB.Length > 0)
            {
                foreach (var rt in blurSampleRTB)
                {
                    rt?.Release();
                }
            }
        }

        #region BoxBlur
        void BoxBlur(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_CommandBuffer.SetGlobalVector(offsetID, new Vector4(
                1.0f / Screen.width * m_Settings.blurRadios,  
                1.0f / Screen.height * m_Settings.blurRadios, 
                0.0f, 0.0f));
            
            m_Descriptor.width = Mathf.Max(2, m_Descriptor.width >> m_Settings.downSample);
            m_Descriptor.height = Mathf.Max(2, m_Descriptor.height >> m_Settings.downSample);
            
            RenderingUtils.ReAllocateIfNeeded(ref blurSampleRTA[0], m_Descriptor, FilterMode.Bilinear, name: blurSampleRTAName + 0);
            RenderingUtils.ReAllocateIfNeeded(ref blurSampleRTB[0], m_Descriptor, FilterMode.Bilinear, name: blurSampleRTBName + 0);
            
            var rtA = m_Settings.downSample == 0 ? m_Target : blurSampleRTA[0];
            var rtB = blurSampleRTB[0];
            for (int i = 0; i < m_Settings.iterations; i++)
            {
                var isA = i % 2 == 0;

                if (isA)
                {
                    Draw(
                        i == 0 ? m_Target : rtA, 
                        rtB, 
                        BlurPassIndex.Box);
                }
                else
                {
                    Draw( 
                        rtB, 
                        i == m_Settings.iterations - 1 ? m_Target : rtA,
                        BlurPassIndex.Box);
                }
            }
        }
        #endregion

        #region GaussianBlur
        void GaussianBlur(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_Descriptor.width = Mathf.Max(2, m_Descriptor.width >> m_Settings.downSample);
            m_Descriptor.height = Mathf.Max(2, m_Descriptor.height >> m_Settings.downSample);
            
            RenderingUtils.ReAllocateIfNeeded(ref blurSampleRTA[0], m_Descriptor, FilterMode.Bilinear, name: blurSampleRTAName + 0);
            RenderingUtils.ReAllocateIfNeeded(ref blurSampleRTB[0], m_Descriptor, FilterMode.Bilinear, name: blurSampleRTBName + 0);
            
            var rtA = m_Settings.downSample == 0 ? m_Target : blurSampleRTA[0];
            var rtB = blurSampleRTB[0];
            for (int i = 0; i < m_Settings.iterations; i++)
            {
                //Horizontal
                m_CommandBuffer.SetGlobalVector(offsetID, new Vector4(
                  1.0f / Screen.width * m_Settings.blurRadios, 0f, 0.0f, 0.0f));
                
                Draw(
                    i == 0 ? m_Target : rtA, 
                    rtB, 
                    BlurPassIndex.Gaussian);
                
                //Vertical
                m_CommandBuffer.SetGlobalVector(offsetID, new Vector4(
                    0, 1.0f / Screen.height * m_Settings.blurRadios, 0.0f, 0.0f));
                
                Draw(
                    rtB, 
                    i == m_Settings.iterations - 1 ? m_Target : rtA, 
                    BlurPassIndex.Gaussian);
            }
        }
        #endregion
        
        void Draw(RTHandle from, RTHandle to, BlurPassIndex index)
        {
            m_CommandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            m_CommandBuffer.SetGlobalTexture(targetID, from);
            RoxamiCommonUtils.DrawFullScreenTriangle(m_CommandBuffer, material, (int)index);
        }
    }
}