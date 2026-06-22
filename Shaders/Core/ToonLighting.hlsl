#ifndef TOON_LIGHTING_INCLUDE
#define TOON_LIGHTING_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/HBAO.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/DeferredPlusShading.hlsl"

TEXTURE2D(_ToonLutMap);
SAMPLER(sampler_ToonLutMap);

half3 ToonMainLightingBased(BRDFData brdfData, Light light, InputData inputData, bool specularHighlightsOff)
{
    half NdotL = saturate(dot(inputData.normalWS, light.direction));
    NdotL *= light.shadowAttenuation * light.distanceAttenuation;
    half3 toonNdotL = SAMPLE_TEXTURE2D_LOD(_ToonLutMap, sampler_ToonLutMap, float2(NdotL, 0), 0).xyz;
    half3 radiance = light.color * toonNdotL;

    half3 brdf = brdfData.diffuse;
    
    #ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
        brdf += brdfData.specular * DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    }
    #endif // _SPECULARHIGHLIGHTS_OFF

    return brdf * radiance;
}

half3 ToonAdditionalLightingBased(BRDFData brdfData, Light light, InputData inputData, bool specularHighlightsOff)
{
    half NdotL = saturate(dot(inputData.normalWS, light.direction));
    NdotL *= light.shadowAttenuation * light.distanceAttenuation;
    half3 radiance = light.color * NdotL;

    half3 brdf = brdfData.diffuse;
    
    #ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
        brdf += brdfData.specular * DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    }
    #endif // _SPECULARHIGHLIGHTS_OFF

    return brdf * radiance;
}

Light ToonGetAdditionalLight(uint i, InputData inputData, half4 shadowMask, RoxamiHBAO aoFactor)
{
    Light light = GetAdditionalLight(i, inputData.positionWS, shadowMask);

    light.color *= aoFactor.dirctionalIntensity;

    return light;
}

half3 ToonDeferredShadingCommon(Light unityLight, InputData inputData, BRDFData brdfData, uint materialFlags)
{
    half3 color = half3(0.0f, 0.0f, 0.0f);

    #if SHADER_API_MOBILE || SHADER_API_SWITCH
    // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
    bool materialSpecularHighlightsOff = false;
    #else
    bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);
    #endif

    color = half3(ToonMainLightingBased(brdfData, unityLight, inputData, materialSpecularHighlightsOff));
    
    return color;
}

half3 ToonDeferredAdditionalLightShadingCommon(Light unityLight, InputData inputData, BRDFData brdfData, uint materialFlags)
{
    half3 color = half3(0.0f, 0.0f, 0.0f);

    #if SHADER_API_MOBILE || SHADER_API_SWITCH
    // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
    bool materialSpecularHighlightsOff = false;
    #else
    bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);
    #endif

    color = half3(ToonAdditionalLightingBased(brdfData, unityLight, inputData, materialSpecularHighlightsOff));
    
    return color;
}

half3 ToonDeferredMainLightShading(InputData inputData, BRDFData brdfData, half4 shadowMask, RoxamiHBAO aoFactor, uint meshRenderingLayers, uint materialFlags)
{
    Light unityLight = GetDeferredMainLight(inputData.positionWS, shadowMask, materialFlags);
    
    // color.w == 0 for MainLight means Subtractive Light (distanceAttenuation)
    // Why MainLightColor.w and AdditionalLightColor.w acts differently?
    #if defined(_DEFERRED_MIXED_LIGHTING)
    // If both lights and geometry are static, then no realtime lighting to perform for this combination.
    [branch] if (_MainLightColor.w == 0 && (materialFlags & kMaterialFlagSubtractiveMixedLighting) != 0)
        return half3(0.0, 0.0, 0.0);
    #endif

    #ifdef _LIGHT_LAYERS
    [branch] if (!IsMatchingLightLayer(unityLight.layerMask, meshRenderingLayers))
        return half3(0.0, 0.0, 0.0);
    #endif

    unityLight.color *= aoFactor.dirctionalIntensity;

    return ToonDeferredShadingCommon(unityLight, inputData, brdfData, materialFlags);
}

half3 ToonDeferredAdditionalLightShading(uint lightIndex, InputData inputData, BRDFData brdfData, half4 shadowMask, RoxamiHBAO aoFactor, uint meshRenderingLayers, uint materialFlags)
{
    Light unityLight = ToonGetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    bool materialReceiveShadowsOff = (materialFlags & kMaterialFlagReceiveShadowsOff) != 0;
    if (materialReceiveShadowsOff)
    {
        unityLight.shadowAttenuation = 1.0;
    }

    // color.w == 1 for AdditionalLight means Subtractive Light
    // Why MainLightColor.w and AdditionalLightColor.w acts differently?
    #if defined(_DEFERRED_MIXED_LIGHTING)
    // If both lights and geometry are static, then no realtime lighting to perform for this combination.
    [branch] if (_AdditionalLightsColor[lightIndex].w > 0 && (materialFlags & kMaterialFlagSubtractiveMixedLighting) != 0)
        return half3(0.0, 0.0, 0.0);
    #endif

    #ifdef _LIGHT_LAYERS
    [branch] if (!IsMatchingLightLayer(unityLight.layerMask, meshRenderingLayers))
        return half3(0.0, 0.0, 0.0);
    #endif

    return ToonDeferredAdditionalLightShadingCommon(unityLight, inputData, brdfData, materialFlags);
}

half3 LightingToonBased(BRDFData brdfData, Light light, InputData inputData)
{
    half NdotL = saturate(dot(inputData.normalWS, light.direction));
    half3 radiance = light.color * (light.shadowAttenuation * light.distanceAttenuation * NdotL);

    half3 brdf = brdfData.diffuse;
    brdf += brdfData.specular * DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);

    return brdf * radiance;
}

#endif