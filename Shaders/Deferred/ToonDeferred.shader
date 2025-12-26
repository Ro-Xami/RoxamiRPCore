Shader "RoxamiRP/Core/ToonDeferred"
{
    Properties
    {
        [Space(10)] [Header(Debug)]
        _DebugNumberMap ("Debug Number Map", 2D) = "white" {}
        _DebugAlpha ("Debug Alpha", Range(0, 1)) = 0.85

//        _LitDirStencilRef ("LitDirStencilRef", Int) = 0
//        _LitDirStencilReadMask ("LitDirStencilReadMask", Int) = 0
//        _LitDirStencilWriteMask ("LitDirStencilWriteMask", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Lit
        Pass
        {
            Name "Toon Stencil Deferred Lit"

            // -------------------------------------
            // Render State Commands
            ZTest NotEqual
            ZWrite Off
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
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, OpenGL ES 2.0, WebGL 2.0.
            #pragma exclude_renderers gles gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment ToonDeferredShading

            // -------------------------------------
            // Defines
            #define _DIRECTIONAL //用于顶点阶段绘制全屏三角形

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile_fragment _ _DEFERRED_MAIN_LIGHT
            //#pragma multi_compile_fragment _ _DEFERRED_FIRST_LIGHT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            //#pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            //#pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            //#pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Deferred/ToonDeferredFragment.hlsl"

            ENDHLSL
        }

        // 1 - DebugClusterLights
        Pass
        {
            Name "Debug Cluster Lights"

            ZTest Always
            ZWrite Off
            Cull Off
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, OpenGL ES 2.0, WebGL 2.0.
            #pragma exclude_renderers gles gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment DebugClusterLights

            // -------------------------------------
            // Defines
            #define _DIRECTIONAL //用于顶点阶段绘制全屏三角形

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Deferred/ToonDeferredFragment.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
