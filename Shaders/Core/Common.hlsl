#ifndef ROXAMIRP_COMMON_INCLUDE
#define ROXAMIRP_COMMON_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float GetReverseDepth(float depth)
{
    #if defined(UNITY_REVERSED_Z)
    float reverseZ = depth;
    #else
    float reverseZ = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
    #endif
    
    return reverseZ;
}

//===========================================================================================//
//=======================================Global Wind=========================================//
//===========================================================================================//
float3 _globalWindDirection;

float4 _globalWindParams;
#define _windStrength _globalWindParams.x
#define _windSpeed _globalWindParams.y
#define _windNoise _globalWindParams.z

void ApplyGlobalWind(inout float3 positionWS, float weight)
{
    //float4x4 v2wMatrix = GetObjectToWorldMatrix();
    float windNoise = _windNoise * (positionWS.x + positionWS.y + positionWS.z);
    float wind = sin(_Time.x * _windSpeed + windNoise) + _windStrength;

    positionWS += wind * _globalWindDirection * weight;
}

#endif