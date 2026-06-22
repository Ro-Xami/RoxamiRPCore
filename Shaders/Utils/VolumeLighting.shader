Shader "RoxamiRP/Utils/VolumeLighting"
{
	Properties
	{
		
	}
	
	SubShader
	{
		HLSLINCLUDE
		#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
		#include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		float _VolumeLighting_RadioBlur_Intensity;
		
		float4 _VolumeLighting_RadioBlur_FilterParams;
		#define _threshold _VolumeLighting_RadioBlur_FilterParams.x
		#define _thresholdKnee _VolumeLighting_RadioBlur_FilterParams.y
		#define _clampMax _VolumeLighting_RadioBlur_FilterParams.z

		float4 _VolumeLighting_RadioBlur_BlurParams;
		#define _blurCount _VolumeLighting_RadioBlur_BlurParams.x
		#define _blurSize _VolumeLighting_RadioBlur_BlurParams.y
		#define _blurCenter _VolumeLighting_RadioBlur_BlurParams.zw

		float4 _VolumeLighting_TexelSize;
		#define _texelSize _VolumeLighting_TexelSize

		ENDHLSL

		Pass
		{
			Name "RayMarchCombine"
			
			Cull Off
			ZWrite Off
			ZTest Always
			Blend One One
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment CombineFragment

			TEXTURE2D_X_HALF(_VolumeLightingTexture);
			SAMPLER(sampler_VolumeLightingTexture);

			half4 CombineFragment (Varyings IN) : SV_TARGET
			{
				half3 volumeLight = SAMPLE_TEXTURE2D_X_LOD(_VolumeLightingTexture, sampler_VolumeLightingTexture, IN.uv, 0);

			    return float4(volumeLight, 1);
			}
			ENDHLSL
		}
	}
}