Shader "RoxamiRP/Utils/DistanceField"
{
	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE

		#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
		#include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		
		//x:center
		//y:near
		//z:far
		float4 _DistanceFieldParams;
		#define _DisCenter _DistanceFieldParams.x
		#define _DisRange _DistanceFieldParams.y
		#define _DisNear _DistanceFieldParams.z
		#define _DisFar _DistanceFieldParams.w
		
		float DistanceMaskFragmentPass(Varyings IN) : SV_Target
		{
			float d = SampleSceneDepth(IN.uv);
			d = LinearEyeDepth(d, _ZBufferParams);
			float dis = distance(d, _DisCenter);
			float near = smoothstep(_DisRange, _DisNear, dis);
			float far = smoothstep(_DisRange, _DisFar, dis);
			float mask = d - _DisCenter < 0? near: far;
		    
		    return mask;
		}

		//Box: xy: (1 / texelSize) * blurRaios
		//Gaussian: 
		float4 _PostBlurOffset;

		TEXTURE2D(_PostBlurInputTexture);
		TEXTURE2D(_BlurOffsetMaskTexture);
		TEXTURE2D(_CameraColorTexture);
		SAMPLER(sampler_LinearClamp);

		half4 SampleBlurTexture(float2 uv)
		{
			return SAMPLE_TEXTURE2D_LOD(_PostBlurInputTexture, sampler_LinearClamp, uv, 0);
		}
		
		half4 SampleCameraColorTexture(float2 uv)
		{
			return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_LinearClamp, uv, 0);
		}
		
		float SampleDistanceFieldMaskTexture(float2 uv)
		{
			return SAMPLE_TEXTURE2D_LOD(_BlurOffsetMaskTexture, sampler_LinearClamp, uv, 0).r;
		}

		// half4 GaussianBlurFragmentPass(Varyings IN) : SV_Target
		// {
		// 	float distanceMask = SampleDistanceFieldMaskTexture(IN.uv);
		// 	float2 uv01 = IN.uv.xyxy + _PostBlurOffset.xyxy * distanceMask * float4(1, 1, -1, -1);
		// 	float2 uv23 = IN.uv.xyxy + _PostBlurOffset.xyxy * distanceMask * float4(1, 1, -1, -1) * 2.0;
		// 	float2 uv45 = IN.uv.xyxy + _PostBlurOffset.xyxy * distanceMask * float4(1, 1, -1, -1) * 6.0;
		// 	
		//     half4 col = 0;
		//     col += SampleBlurTexture(IN.uv) * 0.4;
		//     col += SampleBlurTexture(uv01.xy) * 0.15;
		//     col += SampleBlurTexture(uv01.zw) * 0.15;
		//     col += SampleBlurTexture(uv23.xy) * 0.10;
		//     col += SampleBlurTexture(uv23.zw) * 0.10;
		//     col += SampleBlurTexture(uv45.xy) * 0.05;
		//     col += SampleBlurTexture(uv45.zw) * 0.05;
		//     
		//     return col;
		// }
		
		half4 CombineFragmentPass(Varyings IN) : SV_Target
		{
			float distanceMask = SampleDistanceFieldMaskTexture(IN.uv);
			half4 color = SampleCameraColorTexture(IN.uv);
			half4 blur = SampleBlurTexture(IN.uv);
			
			color = lerp(color, blur, distanceMask);
		    
		    return color;
		}
		
		ENDHLSL

		Pass
		{
			Name "Distance Mask"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment DistanceMaskFragmentPass
			
			ENDHLSL
		}

//		Pass
//		{
//			Name "Gaussian Blur"
//			
//			HLSLPROGRAM
//			
//			#pragma target 3.5
//			#pragma vertex FullScreenTriangle
//			#pragma fragment GaussianBlurFragmentPass
//			
//			ENDHLSL
//		}

		Pass
		{
			Name "Combine DistanceField"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment CombineFragmentPass
			
			ENDHLSL
		}	
	}
}