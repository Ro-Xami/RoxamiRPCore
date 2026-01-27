#ifndef ROXAMIRP_FULLSCREEN_TRIANGLE_INCLUDE
#define ROXAMIRP_FULLSCREEN_TRIANGLE_EXCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCORRD0;
};

void InitializedFullScreenTriangle(uint vertexID : SV_VertexID, inout float4 positionCS, inout float2 uv)
{
    positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    
    uv = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );
    
    #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    #endif
}

Varyings FullScreenTriangle (uint vertexID : SV_VertexID)
{
    Varyings varyings = (Varyings) 0;
    
    InitializedFullScreenTriangle(vertexID, varyings.positionCS, varyings.uv);

    return varyings;
}



#endif