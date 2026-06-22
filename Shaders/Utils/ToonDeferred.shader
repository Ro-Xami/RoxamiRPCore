Shader "RoxamiRP/Core/ToonDeferred"
{
    Properties
    {
        _LitStencilRef ("LitStencilRef", Int) = 0
        _LitStencilReadMask ("LitStencilReadMask", Int) = 0
        _SimpleLitStencilRef ("SimpleLitStencilRef", Int) = 0
        _SimpleLitStencilReadMask ("LitStencilReadMask", Int) = 0
        
        [Header(Toon)]
        _ToonLutMap ("Toon Lut Map", 2D) = "white" {}

        [Space(10)] [Header(Debug)]
        _DebugNumberMap ("Debug Number Map", 2D) = "white" {}
        _DebugAlpha ("Debug Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Deferred+ Shading (Lit)
        Pass
        {
            Name "Deferred Plus Lights (Lit)"

            // -------------------------------------
            // Render State Commands
            ZTest Off
            ZWrite Off
            ZClip false
            Cull Back
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            // -------------------------------------
            // Stencil Settings
            Stencil {
                Ref [_LitStencilRef]
                ReadMask [_LitStencilReadMask]
                Comp Equal
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, OpenGL ES 2.0, WebGL 2.0.
            #pragma exclude_renderers gles gles3 glcore
            
            // -------------------------------------
            // Roxami ToonLighting
            #pragma multi_compile _ _HBAO

            // -------------------------------------
            // Shader Stages
            #pragma vertex Vertex
            #pragma fragment ToonDeferredShading

            // -------------------------------------
            // Defines
            #define _LIT
            #define _CLUSTERED_LIGHTING
            
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            // -------------------------------------
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/DeferredPlusShading.hlsl"
            #include "Packages/roxamirpcore/Shaders/Utils/ToonDeferredFragment.hlsl"

            ENDHLSL
        }

        // 1 - RenderingDebug
        Pass
        {
            Name "Rendering Debug"

            ZTest Always
            ZWrite Off
            Cull Off
            
            Blend One One

            HLSLPROGRAM
            
            #pragma multi_compile _ _RoDebug_None _RoDebug_Albedo _RoDebug_Normal _RoDebug_Metallic _RoDebug_Smoothness _RoDebug_Occlusion _RoDebug_MSA

            #pragma vertex Vertex
            #pragma fragment RenderingDebug
            #include_with_pragmas "Packages/roxamirpcore/Shaders/Utils/ToonDeferredFragment.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
