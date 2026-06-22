Shader "RoxamiRP/Actor/ActorToonLit2D"
{
    Properties
    {
        [PerRendererData] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
        //_ActorLut ("Actor Lut", 2D) = "white" {}

        [Space(10)]
        [Header(Normal Settings)]
        [Toggle(_NORMALMAP)] _BumpMapON ("Enable Normal Map", Float) = 0
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        
        [Space(10)]
        [Header(Metallic Smoothness Occlusion Settings)]
        [Toggle(_METALLICSPECGLOSSMAP)] _MetallicGlossMapON ("Enable MSA Map", Float) = 0
        _MetallicGlossMap("MSA Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Space(10)]
        [Header(Emission Settings)]
        [Toggle(_EMISSION)] _EmissionMapON ("Enable Emission Map", Float) = 0
        _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
       
        [Space(10)]
        [Header(Rendering Settings)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        //[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        
        LOD 300
        
        Pass
        {
            Name "ActorFace"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }
            
            ZWrite Off
            ZTest LEqual
            Cull Off//[_Cull]
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma shader_feature_local _NORMALMAP
            // #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP

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
            
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTERED_LIGHTING

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #pragma vertex ActorToonVertex
            #pragma fragment ActorToon2DForwardFragment
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonPass.hlsl"
            ENDHLSL
        }

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
            Cull Off//[_Cull]
            
            Stencil
            {
                Ref 0
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP

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
            
            #pragma vertex ActorToonVertex
            #pragma fragment ActorToonGBufferPassFragment
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonPass.hlsl"
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
            Cull Off//[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
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
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonLitShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
