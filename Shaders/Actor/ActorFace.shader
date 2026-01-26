Shader "RoxamiRP/Actor/ActorFace"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
        _ActorFaceSdfMap ("SDF Map", 2D) = "white" {}
        _ActorLut ("Actor Lut", 2D) = "white" {}
        
        _ActorFaceDirParams ("Actor Face Dir", Vector) = (1,1,1,1)
        
        //BaseProperties
        //[Space(10)]
        //[Header(Normal Settings)]
        [HideInInspector] [Toggle(_NORMALMAP)] _BumpMapON ("Enable Normal Map", Float) = 0
        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [HideInInspector] _BumpMap("Normal Map", 2D) = "bump" {}
        
        //[Space(10)]
        //[Header(Metallic Smoothness Occlusion Settings)]
        [HideInInspector] [Toggle(_METALLICSPECGLOSSMAP)] _MetallicGlossMapON ("Enable MSA Map", Float) = 0
        [HideInInspector] _MetallicGlossMap("MSA Map", 2D) = "white" {}
        [HideInInspector] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.1
        [HideInInspector] _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        //[Space(10)]
        //[Header(Emission Settings)]
        [HideInInspector] [Toggle(_EMISSION)] _EmissionMapON ("Enable Emission Map", Float) = 0
        [HideInInspector] _EmissionMap("Emission", 2D) = "white" {}
        [HideInInspector] [HDR] _EmissionColor("Color", Color) = (0,0,0)
       
        //[Space(10)]
        //[Header(Rendering Settings)]
        [HideInInspector] [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0.0
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [HideInInspector] [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        
        [HideInInspector] [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [HideInInspector] [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [HideInInspector] [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
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
            Name "ActorFace"
            Tags
            {
                "LightMode" = "ActorForward"
            }
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            Blend One One

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma vertex ActorToonVertex
            #pragma fragment ActorFaceForwardFragment
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
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            Stencil
            {
                Ref 0
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
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

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonInput.hlsl"
            #include "Packages/roxamirpcore/Shaders/Toon/hlsl/ToonLitShadowCasterPass.hlsl"
            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"

}
