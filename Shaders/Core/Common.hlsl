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
//=======================================Global Fog=========================================//
//===========================================================================================//

half3 _RoxamiGlobalFogColor;
float4 _RoxamiGlobalFogParams;//x:Linear Start; y: Linear End; Z: EXP || EXP2

half RoxamiComputeFogLinear(float z , half start , half end)
{        
    //factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    return saturate((start - z) / (start - end));
}

half RoxamiComputeFogEXP(float z , float density)
{        
    //factor = exp(-density*z)
    return 1 - exp(-density * z);
}

half RoxamiComputeFogEXP2(float z , float density)
{        
    //factor = exp(-(density*z)^2)
    half fogFactor = dot(density * z , density * z);
    return 1 - exp(- fogFactor);
}

half RoxamiComputeFogFactor(float z)
{
    #if defined(ROXAMI_FOG_LINEAR) || defined(ROXAMI_FOG_EXP) || defined(ROXAMI_FOG_EXP2)
        #if defined(ROXAMI_FOG_LINEAR)
            return RoxamiComputeFogLinear(z, _RoxamiGlobalFogParams.x, _RoxamiGlobalFogParams.y);
        #elif defined(ROXAMI_FOG_EXP)
            return RoxamiComputeFogEXP(z, _RoxamiGlobalFogParams.z);
        #elif defined(ROXAMI_FOG_EXP2)
            return RoxamiComputeFogEXP2(z, _RoxamiGlobalFogParams.z);
        #endif
    #else
    return 0;
    #endif
}

void RoxamiMixFogColor(inout half3 color, half fogFactor)
{
    #if defined(ROXAMI_FOG_LINEAR) || defined(ROXAMI_FOG_EXP) || defined(ROXAMI_FOG_EXP2)
        color = lerp(color, _RoxamiGlobalFogColor, saturate(fogFactor));
    #endif
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