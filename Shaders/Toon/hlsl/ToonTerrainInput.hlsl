#ifndef ROXAMI_TERRAIN_INPUT_INCLUDED
#define ROXAMI_TERRAIN_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                               Property                                    //
///////////////////////////////////////////////////////////////////////////////
CBUFFER_START(UnityPerMaterial)
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
CBUFFER_END

TEXTURE2D(_Control);    SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

TEXTURE2D(_Normal0);     SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);

TEXTURE2D(_Mask0);      SAMPLER(sampler_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    //#ifdef _NORMALMAP
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    #if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(n);
    #else
    return UnpackNormalScale(n, scale);
    #endif
    //#else
    //return half3(0.0h, 0.0h, 1.0h);
    //#endif
}

void InitializedToonTerrainSurfaceData(inout SurfaceData surfaceData, float2 controlUV, float2 uv0, float2 uv1, float2 uv2, float2 uv3)
{
    half4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV);
    
    half4 albedo = half4(0, 0, 0, 0);
    albedo += SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uv0) * _BaseColor0 * control.r;
    albedo += SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uv1) * _BaseColor1 * control.g;
    albedo += SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uv2) * _BaseColor2 * control.b;
    albedo += SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uv3) * _BaseColor3 * control.a;
    
    half3 normalTS = half3(0, 0, 0);
    normalTS += SampleNormal(uv0, _Normal0, sampler_Normal0, _NormalScale0) * control.r;
    normalTS += SampleNormal(uv1, _Normal1, sampler_Normal0, _NormalScale1) * control.g;
    normalTS += SampleNormal(uv2, _Normal2, sampler_Normal0, _NormalScale2) * control.b;
    normalTS += SampleNormal(uv3, _Normal3, sampler_Normal0, _NormalScale3) * control.a;
    
    half4 mask = half4(0, 0, 0, 0);
    mask += SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uv0) * half4(_Smoothness0, _Metallic0, _OcclusionStrength0, 1) * control.r;
    mask += SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uv1) * half4(_Smoothness1, _Metallic1, _OcclusionStrength1, 1) * control.g;
    mask += SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uv2) * half4(_Smoothness2, _Metallic2, _OcclusionStrength2, 1) * control.b;
    mask += SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uv3) * half4(_Smoothness3, _Metallic3, _OcclusionStrength3, 1) * control.a;

    surfaceData.albedo = albedo.rgb;
    surfaceData.normalTS = normalTS.rgb;
    surfaceData.smoothness = mask.r;
    surfaceData.metallic = mask.g;
    surfaceData.occlusion = mask.b;
    surfaceData.alpha = 1;
    surfaceData.specular = half3(1, 1, 1);
}

#endif
