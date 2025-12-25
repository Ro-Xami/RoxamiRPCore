#ifndef ROXAMI_CLUSTERED_LIGHTING_CORE_INCLUDE
#define ROXAMI_CLUSTERED_LIGHTING_CORE_INCLUDE

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

#endif