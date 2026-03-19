#ifndef ROXAMI_CLUSTERED_LIGHTING_CORE_INCLUDE
#define ROXAMI_CLUSTERED_LIGHTING_CORE_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/ToonLighting.hlsl"

uint _MaxClusterLightIndex;

#ifdef _USE_RW_BUFFER
RWBuffer<int> _ClusterLightCountBuffer;
RWBuffer<int> _ClusterLightIndexBuffer;
#else
Buffer<int> _ClusterLightCountBuffer;
Buffer<int> _ClusterLightIndexBuffer;
#endif

float4 _ClusterCount;

//从3维id转换成1维id
uint GetIdFrom2D(uint2 id)
{
    return
        id.x +
        id.y * _ClusterCount.x;
}

uint GetIdFormClusterSpace(float2 screenPos)
{
    uint2 id2d = screenPos * _ClusterCount.xy;
    return GetIdFrom2D(id2d);
}

//获得裁剪后的灯光总数量
uint GetClusteredLightCount(uint id)
{
    return _ClusterLightCountBuffer[id];
}

uint GetClusteredLightStart(uint id)
{
    return id * _MaxClusterLightIndex;
}

uint GetClusteredLightIndex(uint id)
{
    return _ClusterLightIndexBuffer[id];
}

half3 GetClusteredLightingBased(float2 screen_uv, BRDFData brdfData, InputData inputData)
{
    half3 color = 0;
    
    uint clusterID = GetIdFormClusterSpace(screen_uv);
    uint clusteredLightStart = GetClusteredLightStart(clusterID);
    int clusteredLightCount = GetClusteredLightCount(clusterID);
    UNITY_LOOP
    for (int index = 0; index < clusteredLightCount; index++)
    {
        uint clusteredLightIndex = GetClusteredLightIndex(clusteredLightStart + index);
        Light additionalLight = GetAdditionalPerObjectLight(clusteredLightIndex, inputData.positionWS);
        color.rgb += LightingToonBased(brdfData, additionalLight, inputData);
    }
    
    return color;
}

half3 GetClusteredLightingDistanceAttenuation(float2 screen_uv, float3 positionWS)
{
    half3 color = 0;
    
    uint clusterID = GetIdFormClusterSpace(screen_uv);
    uint clusteredLightStart = GetClusteredLightStart(clusterID);
    int clusteredLightCount = GetClusteredLightCount(clusterID);
    UNITY_LOOP
    for (int index = 0; index < clusteredLightCount; index++)
    {
        uint clusteredLightIndex = GetClusteredLightIndex(clusteredLightStart + index);
        Light additionalLight = GetAdditionalPerObjectLight(clusteredLightIndex, positionWS);
        color.rgb += additionalLight.color.rgb * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
    }
    
    return color;
}

#endif