#ifndef _TOON_DEFERRED_FRAGMENT
#define _TOON_DEFERRED_FRAGMENT

#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Utils/StencilDeferred.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/ToonLighting.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/ClusteredLightingCore.hlsl"

float2 GetScreenUV(Varyings input)
{
    float2 screen_uv = (input.screenUV.xy / input.screenUV.z);

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    float2 undistorted_screen_uv = screen_uv;
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        screen_uv = input.positionCS.xy * _ScreenSize.zw;
    }
    #endif

    return screen_uv;
}

TEXTURE2D_X_HALF(_HBAoTexture);
half _HbaoDirectionalIntensity;

float SampleHBAO(float2 screenUV)
{
    return saturate(1 - SAMPLE_TEXTURE2D_X_LOD(_HBAoTexture, my_point_clamp_sampler, screenUV, 0).r);
}

half4 ToonDeferredShading(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screen_uv = GetScreenUV(input);

    // Using SAMPLE_TEXTURE2D is faster than using LOAD_TEXTURE2D on iOS platforms (5% faster shader).
    // Possible reason: HLSLcc upcasts Load() operation to float, which doesn't happen for Sample()?
    float d        = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, my_point_clamp_sampler, screen_uv, 0).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
    half4 gbuffer0 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer1 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer2 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, screen_uv, 0);

    half surfaceDataOcclusion = gbuffer1.a;
    uint materialFlags = UnpackMaterialFlags(gbuffer0.a);

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        input.positionCS.xy = undistorted_screen_uv * _ScreenSize.xy;
    }
#endif

    #if defined(USING_STEREO_MATRICES)
    int eyeIndex = unity_StereoEyeIndex;
    #else
    int eyeIndex = 0;
    #endif
    float4 posWS = mul(_ScreenToWorld[eyeIndex], float4(input.positionCS.xy, d, 1.0));
    posWS.xyz *= rcp(posWS.w);

// #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
//         AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screen_uv);
// #endif
    
    half ao = surfaceDataOcclusion;
#if defined(_HBAO)
    ao *= SampleHBAO(screen_uv);
#endif

    InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);

    bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);

    BRDFData brdfData = BRDFDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

    Light mainLight = GetToonDeferredMainLight(inputData.positionWS, screen_uv);
    half3 mainLightColor = MainLightingToonBased(brdfData, mainLight, inputData);
    mainLightColor *= LerpWhiteTo(ao, _HbaoDirectionalIntensity);

    half3 additionalLightColor = 0;
    uint clusterID = GetIdFormClusterSpace(screen_uv, d);
    uint clusteredLightStart = GetClusteredLightStart(clusterID);
    int clusteredLightCount = GetClusteredLightCount(clusterID);
    UNITY_LOOP
    for (int index = 0; index < clusteredLightCount; index++)
    {
        uint clusteredLightIndex = GetClusteredLightIndex(clusteredLightStart + index);
        Light additionalLight = GetAdditionalPerObjectLight(clusteredLightIndex, inputData.positionWS);
        additionalLightColor += LightingToonBased(brdfData, additionalLight, inputData);
    }

    half4 color = 0;
    color.rgb += mainLightColor;
    color.rgb += additionalLightColor;
    color.a = ao;

    return color;
}

//===========================================================================//
//=========================ConvolutionOutline================================//
//===========================================================================//

// Sobel卷积核
static const float sobelX[9] = {
    -1.0, 0.0, 1.0,
    -2.0, 0.0, 2.0,
    -1.0, 0.0, 1.0
};

static const float sobelY[9] = {
    -1.0, -2.0, -1.0,
     0.0,  0.0,  0.0,
     1.0,  2.0,  1.0
};

// 获取深度值（线性深度）
float GetLinearDepth(float2 uv)
{
    float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, my_point_clamp_sampler, uv, 0).x;
    return LinearEyeDepth(depth, _ZBufferParams);
}

// 应用Sobel卷积
float ApplySobel(float2 uv, float2 texelSize)
{
    float depthSumX = 0.0;
    float depthSumY = 0.0;
    
    int index = 0;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * texelSize * _ConvolutionOutline_OutlineWidth;
            float2 sampleUV = uv + offset;
            float depth = GetLinearDepth(sampleUV);
            
            depthSumX += depth * sobelX[index];
            depthSumY += depth * sobelY[index];
            
            index++;
        }
    }
    
    // 计算梯度幅度
    float gradient = sqrt(depthSumX * depthSumX + depthSumY * depthSumY);
    return gradient;
}

half4 ConvolutionOutlineFragment(Varyings input) : SV_Target
{
    float2 screenUV = GetScreenUV(input);

    float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

    float edgeStrength = ApplySobel(screenUV, texelSize);

    edgeStrength = step(_ConvolutionOutline_DepthThreshold, edgeStrength) * edgeStrength;

    edgeStrength *= _ConvolutionOutline_OutlineIntensity;

    edgeStrength = saturate(edgeStrength);

    half3 outlineColor = lerp(half3(1, 1, 1), _ConvolutionOutline_Color.rgb, edgeStrength);
    
    return half4(outlineColor, 1);
}

//===========================================================================//
//===============================ClearStencil================================//
//===========================================================================//
half4 ClearStencilFragmentPass(Varyings input) : SV_Target
{
    return 0;
}

//===========================================================================//
//=========================DebugClusterLights================================//
//===========================================================================//
#define _Number 100
#define _NumberX 10
#define _NumberY 10

float _ClusteredDebugIndexZ;

half SampleNumber(uint id, float2 screenUV)
{
    uint number = id + 1;

    uint tileX = (number - 1) % _NumberX;
    uint tileY = (number - 1) / _NumberX;

    float2 tileSize = float2(1.0 / _NumberX, 1.0 / _NumberY);

    float2 tileMinUV;
    tileMinUV.x = tileX * tileSize.x;
    tileMinUV.y = 1.0 - (tileY + 1) * tileSize.y; // 如果你的图是左上为1

    float2 localUV = frac(screenUV * _ClusterCount.xy);
    float2 sampleUV = tileMinUV + localUV * tileSize;

    return SAMPLE_TEXTURE2D(
        _DebugNumberMap,
        sampler_DebugNumberMap,
        sampleUV
    ).r;
}

half4 DebugClusterLights(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    // return 0;

    float2 screenUV = GetScreenUV(input);
    
    uint clusterID = GetIdFormClusterSpace(screenUV, saturate(_ClusteredDebugIndexZ));
    uint clusteredLightCount = GetClusteredLightCount(clusterID);
    
    half3 color = lerp(half3(0, 0, 1), half3(1, 0, 0), (float)clusteredLightCount / (float)_MaxClusterLightIndex);
    
    half number = SampleNumber(clusteredLightCount, screenUV);
    
    color = lerp(color, half3(1, 1, 1), number);
    
    return half4(color, _DebugAlpha);
}

//===========================================================================//
//============================Rendering Debug================================//
//===========================================================================//
half4 RenderingDebug(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screen_uv = GetScreenUV(input);

    // Using SAMPLE_TEXTURE2D is faster than using LOAD_TEXTURE2D on iOS platforms (5% faster shader).
    // Possible reason: HLSLcc upcasts Load() operation to float, which doesn't happen for Sample()?
    float d        = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, my_point_clamp_sampler, screen_uv, 0).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
    half4 gbuffer0 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer1 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer2 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, screen_uv, 0);

    half surfaceDataOcclusion = gbuffer1.a;
    uint materialFlags = UnpackMaterialFlags(gbuffer0.a);

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        input.positionCS.xy = undistorted_screen_uv * _ScreenSize.xy;
    }
#endif

    #if defined(USING_STEREO_MATRICES)
    int eyeIndex = unity_StereoEyeIndex;
    #else
    int eyeIndex = 0;
    #endif
    float4 posWS = mul(_ScreenToWorld[eyeIndex], float4(input.positionCS.xy, d, 1.0));
    posWS.xyz *= rcp(posWS.w);

// #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
//         AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screen_uv);
// #endif

    InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);

    bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);

    BRDFData brdfData = BRDFDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
    
    SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, 0);
    
    half4 color = 0;
    color.a = d == 0? 0: 1;
    
    #if defined(_RoDebug_None)
    color = 0;
    #elif defined(_RoDebug_Albedo)
    color.rgb = surfaceData.albedo;
    #elif defined(_RoDebug_Normal)
    half3 normal = PackNormal(inputData.normalWS) * 0.5f + 0.5f;
    normal.y = 1 - normal.y;
    color.rgb = normal;
    #elif defined(_RoDebug_Metallic)
    color.rgb = surfaceData.metallic.xxx;
    #elif defined(_RoDebug_Smoothness)
    color.rgb = surfaceData.smoothness.xxx;
    #elif defined(_RoDebug_Occlusion)
    color.rgb = surfaceData.occlusion.xxx;
    #elif defined(_RoDebug_MSA)
    color.rgb = half3(surfaceData.metallic, surfaceData.smoothness, surfaceData.occlusion);
    #endif

    return color;
}

#endif