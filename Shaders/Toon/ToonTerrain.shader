Shader "RoxamiRP/Scene/ToonTerrain"
{
    Properties
    {
        [KeywordEnum(UV, WorldPosition)] _SampleInput("Sample Input", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        
        [Spce(20)] [Header(Textures)]
        _BaseColor0 ("Base Color 0", Color) = (1, 1, 1, 1)
        _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [NoScaleOffset] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [NoScaleOffset] _Mask0("Mask 0 (R)", 2D) = "white" {}
        _NormalScale0 ("Normal Scale 0", Float) = 1.0
        _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0
        _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        _OcclusionStrength0 ("Occlusion Strength 0", Range(0.0, 1.0)) = 1.0
        
        [Space(20)]
        _BaseColor1 ("Base Color 1", Color) = (1, 1, 1, 1)
        _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [NoScaleOffset] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [NoScaleOffset] _Mask1("Mask 1 (G)", 2D) = "white" {}
        _NormalScale1 ("Normal Scale 1", Float) = 1.0
        _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.0
        _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        _OcclusionStrength1 ("Occlusion Strength 1", Range(0.0, 1.0)) = 1.0
        
        [Space(20)]
        _BaseColor2 ("Base Color 2", Color) = (1, 1, 1, 1)
        _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [NoScaleOffset] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [NoScaleOffset] _Mask2("Mask 2 (B)", 2D) = "white" {}
        _NormalScale2 ("Normal Scale 2", Float) = 1.0
        _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        _OcclusionStrength2 ("Occlusion Strength 2", Range(0.0, 1.0)) = 1.0
        
        [Space(20)]
        _BaseColor3 ("Base Color 3", Color) = (1, 1, 1, 1)
        _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [NoScaleOffset] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [NoScaleOffset] _Mask3("Mask 3 (A)", 2D) = "white" {}
        _NormalScale3 ("Normal Scale 3", Float) = 1.0
        _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5
        _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        _OcclusionStrength3 ("Occlusion Strength 3", Range(0.0, 1.0)) = 1.0
        
        [Space(20)]
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        _Control("Control (RGBA)", 2D) = "red" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        
        LOD 300

        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            
            Stencil
            {
                Ref 100
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex ToonTerrainGBufferPassVertex
            #pragma fragment ToonTerrainGBufferPassFragment

            // -------------------------------------
            // Material Keywords
            #define _NORMALMAP
            #pragma shader_feature_local _SAMPLEINPUT_UV _SAMPLEINPUT_WORLDPOSITION
            //#pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature_local_fragment _EMISSION
            //#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            //#pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonTerrainInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonTerrainGBufferPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature_local _ALPHATEST_ON
            //#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Universal Pipeline keywords

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonTerrainInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonLitShadowCasterPass.hlsl"
            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
