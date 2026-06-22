#ifndef ROXAMIRP_HBAO_INCLUDE
#define ROXAMIRP_HBAO_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct RoxamiHBAO
{
    float dirctionalIntensity;
    float inDirectionalIntensity;
};

TEXTURE2D_X_HALF(_HBAoTexture);
SAMPLER(sampler_HBAoTexture);

half4 _HbaoGlobalParams;
#define _HbaoDirectionalIntensity       saturate(_HbaoGlobalParams.x)
#define  _HbaoInDirectionalIntensity    saturate(_HbaoGlobalParams.y)

half SampleHBAO(float2 screenUV)
{
    return saturate(
        1 - SAMPLE_TEXTURE2D_X(_HBAoTexture, sampler_HBAoTexture, UnityStereoTransformScreenSpaceTex(screenUV)).x
        );
}

RoxamiHBAO GetRoxamiHBAO(float2 screeUV)
{
    half ao = SampleHBAO(screeUV);

#ifdef _HBAO
    half dir = LerpWhiteTo(ao, _HbaoDirectionalIntensity);
    half inDir = LerpWhiteTo(ao, _HbaoInDirectionalIntensity);
#else
    half dir = 1;
    half inDir = 1;
#endif
    
    RoxamiHBAO output;
    output.dirctionalIntensity = dir;
    output.inDirectionalIntensity = inDir;
    
    return output;
}

#endif