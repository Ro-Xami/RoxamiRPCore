Shader "RoxamiRP/Core/ToonDeferred"
{
    Properties
    {
        [Header(Toon)]
        _ToonLutMap ("Toon Lut Map", 2D) = "white" {}
        
        [Space(10)] [Header(Outline)]
        _ConvolutionOutline_Color ("Outline Color", Color) = (0, 0, 0, 0)
        _ConvolutionOutline_OutlineWidth ("Outline Width", Range(0.1, 10.0)) = 1.0
        _ConvolutionOutline_DepthThreshold ("Depth Threshold", Range(0.001, 0.1)) = 0.01
        _ConvolutionOutline_OutlineIntensity ("Outline Intensity", Range(0.0, 10.0)) = 1.0

        [Space(10)] [Header(Debug)]
        _DebugNumberMap ("Debug Number Map", 2D) = "white" {}
        _DebugAlpha ("Debug Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #pragma target 4.5
        // Deferred Rendering Path does not support the OpenGL-based graphics API:
        // Desktop OpenGL, OpenGL ES 3.0, OpenGL ES 2.0, WebGL 2.0.
        #pragma exclude_renderers gles gles3 glcore

        // -------------------------------------
        // Defines
        #define _DIRECTIONAL //用于顶点阶段绘制全屏三角形

        // -------------------------------------
        // Properties
        TEXTURE2D(_DebugNumberMap);
        SAMPLER(sampler_DebugNumberMap);

        CBUFFER_START(UnityPerMaterial)

        half4 _ConvolutionOutline_Color;
        float _ConvolutionOutline_OutlineWidth;
        float _ConvolutionOutline_DepthThreshold;
        float _ConvolutionOutline_OutlineIntensity;

        half _DebugAlpha;
        
        CBUFFER_END
        
        ENDHLSL

        // 0 - Lit
        Pass
        {
            Name "Toon Stencil Deferred Lit"

            // -------------------------------------
            // Render State Commands
            ZWrite Off
            ZTest Always
            Cull Off
            
            Blend One Zero
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref 100
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _DEFERRED_MAIN_LIGHT
            //#pragma multi_compile_fragment _ _DEFERRED_FIRST_LIGHT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma vertex Vertex
            #pragma fragment ToonDeferredShading
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Deferred/ToonDeferredFragment.hlsl"

            ENDHLSL
        }

        // 1 - ConvolutionOutline
        Pass
        {
            Name "ConvolutionOutline"

            ZWrite Off
            ZTest Always
            Cull Off

            Blend DstColor Zero

            ColorMask RGB
            
            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref 100
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment ConvolutionOutlineFragment
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Deferred/ToonDeferredFragment.hlsl"

            ENDHLSL
        }

        // 2 - DebugClusterLights
        Pass
        {
            Name "Debug Cluster Lights"

            ZTest Always
            ZWrite Off
            Cull Off
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment DebugClusterLights
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Deferred/ToonDeferredFragment.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
