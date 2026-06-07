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
uint _MaxClusterCount;

//从3维id转换成1维id
uint GetIdFrom3D(uint3 id3D)
{
    uint id =
        id3D.x +
        id3D.y * _ClusterCount.x + 
        id3D.z * _ClusterCount.y * _ClusterCount.z;
    
    return min(max(id, 0), _MaxClusterCount);
}

uint GetIdFormClusterSpace(float2 screenPos, float z)
{
    uint2 idXY = screenPos * _ClusterCount.xy;
    uint idZ = (uint)z * _ClusterCount.z;
    uint3 id3D = uint3(idXY, idZ);
    
    return GetIdFrom3D(id3D);
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

half3 GetClusteredLightingBased(float2 screen_uv, float z, BRDFData brdfData, InputData inputData)
{
    half3 color = 0;
    
    uint clusterID = GetIdFormClusterSpace(screen_uv, z);
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

half3 GetClusteredLightingDistanceAttenuation(float2 screen_uv, float z, float3 positionWS)
{
    half3 color = 0;
    
    uint clusterID = GetIdFormClusterSpace(screen_uv, z);
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