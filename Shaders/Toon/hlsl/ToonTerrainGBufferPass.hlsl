#ifndef ROXAMI_TERRAIN_GBUFFER_PASS_INCLUDED
#define ROXAMI_TERRAIN_GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/RoxamiGBuffer.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

// keep this file in sync with LitForwardPass.hlsl

struct Attributes
{
    float4 positionOS           : POSITION;
    float3 normalOS             : NORMAL;
    float4 tangentOS            : TANGENT;
    float2 texcoord             : TEXCOORD0;
    float2 staticLightmapUV     : TEXCOORD1;
    float2 dynamicLightmapUV    : TEXCOORD2;
    float4 color                : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 controlUV                : TEXCOORD0;
    float3 positionWS               : TEXCOORD1;
    half3 normalWS                  : TEXCOORD2;
    half4 tangentWS                 : TEXCOORD3;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
    float4 color                    : TEXCOORD5;
    float4 uv01                     : TEXCOORD6;
    float4 uv23                     : TEXCOORD7;
    float4 positionCS               : SV_POSITION;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;

    #if defined(_NORMALMAP) || defined(_DETAIL)
        float sgn = input.tangentWS.w;      // should be either +1 or -1
        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
        inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    #else
        inputData.normalWS = input.normalWS;
    #endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    //not use
    inputData.shadowCoord = float4(0, 0, 0, 0);
    inputData.fogCoord = 0.0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.shadowMask = 1;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////
Varyings ToonTerrainGBufferPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.controlUV = input.texcoord;
    
    float2 sampleUV = input.texcoord;
    #ifdef _SAMPLEINPUT_UV
    #else
    float3 face = float3(0, 0, 0);
    half3 absNormal = abs(output.normalWS);
    face.x = step(absNormal.y, absNormal.x) * step(absNormal.z, absNormal.x);
    face.y = step(absNormal.z, absNormal.y) * (1 - face.x);
    face.z = 1 - face.x - face.y;
    sampleUV = output.positionWS.xy * face.z + output.positionWS.zy * face.x + output.positionWS.xz * face.y;
    #endif
    output.uv01.xy = TRANSFORM_TEX(sampleUV, _Splat0);
    output.uv01.zw = TRANSFORM_TEX(sampleUV, _Splat1);
    output.uv23.xy = TRANSFORM_TEX(sampleUV, _Splat2);
    output.uv23.zw = TRANSFORM_TEX(sampleUV, _Splat3);
    
    output.color = input.color;

    return output;
}

// Used in Standard (Physically Based) shader
FragmentOutput ToonTerrainGBufferPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData = (SurfaceData) 0;
    InitializedToonTerrainSurfaceData(surfaceData, input.controlUV, input.uv01.xy, input.uv01.zw, input.uv23.xy, input.uv23.zw);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);
    
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);

    return BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
}

#endif
