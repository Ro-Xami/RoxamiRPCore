Shader "RoxamiRP/Scene/VolumeFog"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
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
            Name "VolumeFog"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            
            #pragma vertex VolumeFogVertex
            #pragma fragment VolumeFogFragment
            #include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)

            half4 _BaseColor;
            
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 scrPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VolumeFogVertex(Attributes input)
            {
                Varyings output = (Varyings) 0;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.scrPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            half4 VolumeFogFragment(Varyings input) : SV_Target
            {
                float2 screenUV = input.scrPos.xy / input.scrPos.w;
                
                float depth = SampleSceneDepth(screenUV);
                depth = LinearEyeDepth(depth, _ZBufferParams);
                //depth = GetReverseDepth(depth);
                
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float attanuation = length(_WorldSpaceCameraPos - positionWS);
                
                return half4(depth.xxx, 1);
            }

            
            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
