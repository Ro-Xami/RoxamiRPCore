#ifndef ROXAMI_LIT_INPUT_INCLUDED
#define ROXAMI_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                               Property                                    //
///////////////////////////////////////////////////////////////////////////////
CBUFFER_START(UnityPerMaterial)

float4 _BaseMap_ST;
half4 _BaseColor;
half _BasePower;

half _Smoothness;
half _Metallic;
half _OcclusionStrength;
half _BumpScale;
half4 _EmissionColor;
half _Cutoff;

//Terrain
half4 _BaseColor0;
half4 _Splat0_ST;
half _NormalScale0;
half _Smoothness0;
half _Metallic0;
half _OcclusionStrength0;

half4 _BaseColor1;
half4 _Splat1_ST;
half _NormalScale1;
half _Smoothness1;
half _Metallic1;
half _OcclusionStrength1;

half4 _BaseColor2;
half4 _Splat2_ST;
half _NormalScale2;
half _Smoothness2;
half _Metallic2;
half _OcclusionStrength2;

half4 _BaseColor3;
half4 _Splat3_ST;
half _NormalScale3;
half _Smoothness3;
half _Metallic3;
half _OcclusionStrength3;


//ApplyWind
half _WindWeightFactor;
half4 _GrassColor;

CBUFFER_END

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_WindWeightMask);     SAMPLER(sampler_WindWeightMask);

float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
    half alpha = albedoAlpha * color.a;
    #else
    half alpha = color.a;
    #endif

    alpha = AlphaDiscard(alpha, cutoff);

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    #ifdef _TOON_LIT_2D
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return n.xyz * 2 - 1;
    #else
    
    #ifdef _NORMALMAP
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    #if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(n);
    #else
    return UnpackNormalScale(n, scale);
    #endif
    #else
    return half3(0.0h, 0.0h, 1.0h);
    #endif
    
    #endif
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
    #ifndef _EMISSION
    return emissionColor;
    #else
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
    #endif
}

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss = half4(1, 1, 1, 1);

    #ifdef _METALLICSPECGLOSSMAP
    specGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    #endif

    specGloss.r *= _Metallic;
    specGloss.g *= _Smoothness;
    specGloss.b *= _OcclusionStrength;

    return specGloss;
}
#endif
