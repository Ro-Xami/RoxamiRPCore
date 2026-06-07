#ifndef ROXAMI_ACTOR_GBUFFER_PASS_INCLUDED
#define ROXAMI_ACTOR_GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/RoxamiGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/roxamirpcore/Shaders/Actor/hlsl/ActorToonInput.hlsl"
#include "Packages/roxamirpcore/Shaders/Core/ClusteredLightingCore.hlsl"

#define _RoxamiF0 0.04f

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
    float2 uv                       : TEXCOORD0;
    float3 positionWS               : TEXCOORD1;
    half3 normalWS                  : TEXCOORD2;
    half4 tangentWS                 : TEXCOORD3;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
    float4 color                    : TEXCOORD5;
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

inline void InitializeStandardLitSurfaceData(Varyings input, out SurfaceData outSurfaceData)
{
    float2 uv = input.uv;
    
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.smoothness = specGloss.g;
    outSurfaceData.occlusion = specGloss.b;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
    
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

///////////////////////////////////////////////////////////////////////////////
//                               Vertex functions                            //
///////////////////////////////////////////////////////////////////////////////
VertexPositionInputs m_GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input = (VertexPositionInputs) 0;
    input.positionWS = TransformObjectToWorld(positionOS);
    
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

Varyings ActorToonVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = m_GetVertexPositionInputs(input.positionOS.xyz);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.color = input.color;

    return output;
}

///////////////////////////////////////////////////////////////////////////////
//                             Actor Functions                               //
///////////////////////////////////////////////////////////////////////////////
float SDF_NoL(float2 uv, float3 lightDir)
{
    float4 leftLightShadow = SAMPLE_TEXTURE2D(_ActorFaceSdfMap, sampler_ActorFaceSdfMap, float2(1 - uv.x , 1 - uv.y));
    float4 rightLightShadow = SAMPLE_TEXTURE2D(_ActorFaceSdfMap, sampler_ActorFaceSdfMap, float2(uv.x , 1 - uv.y));
    float2 rightDir_XZ = normalize(_ActorFaceRightDirXZ);
    float2 lightDir_XZ = normalize(lightDir.xz);
    float2 frontDir_XZ = normalize(_ActorFaceFrontDirXZ);
    float isFront = dot(lightDir_XZ , frontDir_XZ);
    float isRight = dot(lightDir.xz , rightDir_XZ);
    float sdf_LightShadow = isRight > 0 ? rightLightShadow.r : leftLightShadow.r;
    //float NoL = step(1- isFront, sdf_LightShadow); //Hard
    float NoL = saturate(sdf_LightShadow + isFront); //Soft
    
    return NoL;
}

float DepthRim(InputData inputData)
{
    float depth = SampleSceneDepth(inputData.normalizedScreenSpaceUV);
    float3 normalVS = TransformWorldToViewDir(inputData.normalWS, true);
    float2 signDir = normalVS.xy;
    float2 offestSamplePos = inputData.normalizedScreenSpaceUV + _ActorRimOffest * _CameraDepthTexture_TexelSize.xy / inputData.positionCS.w * signDir;
    float offsetDepth = SampleSceneDepth(offestSamplePos);
    //Rim
    float linear01EyeOffestDepth = Linear01Depth(offsetDepth , _ZBufferParams);
    float linear01EyeDepth = Linear01Depth(depth , _ZBufferParams);
    float depthDiffer = linear01EyeOffestDepth - linear01EyeDepth;
    float rim = step(_ActorRimThreshold * 0.001, depthDiffer);
    return rim;
}

///////////////////////////////////////////////////////////////////////////////
//                            Fragment Functions                             //
///////////////////////////////////////////////////////////////////////////////
FragmentOutput ActorToonGBufferPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input, surfaceData);

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

half4 ActorFaceForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
    
    InputData inputData;
    InitializeInputData(input, half3(0, 0, 1), inputData);
    
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
    
    Light mainLight = GetMainLight();
    half NdotL = SDF_NoL(input.uv, mainLight.direction);
    half3 toonNdotL = SAMPLE_TEXTURE2D(_ActorLut, sampler_ActorLut, half2(NdotL.x, 0));

    half rim = DepthRim(inputData) * step(0.5, NdotL);
    
    half3 color = baseColor.rgb * mainLight.color * toonNdotL + rim.xxx;

    return half4(color, 1);
}

half4 ActorToonForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input, surfaceData);
    
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
    
    Light mainLight = GetMainLight();
    half NdotL = saturate(dot(mainLight.direction, inputData.normalWS));
    half3 toonNdotL = SAMPLE_TEXTURE2D(_ActorLut, sampler_ActorLut, half2(NdotL.x, 0));

    half rim = DepthRim(inputData) * step(0.5, NdotL);
    
    half3 spec = _RoxamiF0 * DirectBRDFSpecular(brdfData, inputData.normalWS, mainLight.direction, inputData.viewDirectionWS);
    
    half3 color = surfaceData.albedo.rgb * mainLight.color * toonNdotL + rim.xxx + spec * NdotL;
    
    return half4(color, 1);
}

half4 ActorToon2DForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input, surfaceData);
    
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    // BRDFData brdfData;
    // InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
    
    Light mainLight = GetMainLight();
    //half NdotL = saturate(dot(mainLight.direction, inputData.normalWS));
    //half3 spec = _RoxamiF0 * DirectBRDFSpecular(brdfData, inputData.normalWS, mainLight.direction, inputData.viewDirectionWS);
    
    half3 additionalLightColor = 
        GetClusteredLightingDistanceAttenuation(
            inputData.normalizedScreenSpaceUV, 
            input.positionCS.z / input.positionCS.w,
            inputData.positionWS
            );
    
    half4 color = 0;
    color.rgb += max(mainLight.color, 0.35f) * surfaceData.albedo.rgb;
    color.rgb += additionalLightColor * surfaceData.albedo.rgb;
    color.a = surfaceData.alpha;
    
    return color;
}

#endif
