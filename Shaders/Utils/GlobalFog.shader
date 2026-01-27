Shader "RoxamiRP/Utils/GlobalFog"
{
    Properties
    {

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent-100"
            "RenderPipeline" = "UniversalPipeline"
            //"UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }
        
        LOD 300

        Pass
        {
            Name "GlobalFog"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            ZWrite Off
            ZTest Always
            Cull Off
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma multi_compile _ ROXAMI_FOG_NONE ROXAMI_FOG_LINEAR ROXAMI_FOG_EXP ROXAMI_FOG_EXP2
            #pragma vertex FullScreenTriangle
            #pragma fragment GlobalFogFragment
            #include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
            #include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 GlobalFogFragment(Varyings input) : SV_Target
            {
                float2 screenUV = input.uv;
                
                float depth = SampleSceneDepth(screenUV);
                depth = LinearEyeDepth(depth, _ZBufferParams);
                
                half fogFactor = RoxamiComputeFogFactor(depth);
                
                return half4(_RoxamiGlobalFogColor, fogFactor);
            }

            ENDHLSL
        }

    }

    //FallBack "Hidden/Universal Render Pipeline/FallbackError"
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
