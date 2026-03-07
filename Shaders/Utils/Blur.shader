Shader "RoxamiRP/Utils/Blur"
{
	Properties
	{
		//[Toggle(_PostBlur_Mask)] _PostBlur_Mask_ON ("PostBlur Mask ON", Float) = 0
	}
	
	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE
		
		#pragma multi_compile _ _PostBlur_Mask

		#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
		#include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"

		//Box: xy: (1 / texelSize) * blurRaios
		//Gaussian: 
		float4 _PostBlurOffset;

		TEXTURE2D(_PostBlurInputTexture);
		TEXTURE2D(_BlurOffsetMaskTexture);
		SAMPLER(sampler_LinearClamp);

		half4 SampleBlurTexture(float2 uv)
		{
			return SAMPLE_TEXTURE2D_LOD(_PostBlurInputTexture, sampler_LinearClamp, uv, 0);
		}
		
		float4 GetOffset(float2 uv)
		{
			float4 offset = _PostBlurOffset;
			
			#if defined(_PostBlur_Mask)
			float mask = SAMPLE_TEXTURE2D_LOD(_BlurOffsetMaskTexture, sampler_LinearClamp, uv, 0).r;
			 offset *= mask;
			#endif
			
			return offset;
		}

		//==============================================================================//
		//======================================Box=====================================//
		//==============================================================================//
		half4 BoxBlurFragmentPass(Varyings IN) : SV_Target
		{
			float2 uv = IN.uv;
			float4 offset = GetOffset(uv);
			
			float2 uv0 = uv + offset.xy * float2( 1,  1);
			float2 uv1 = uv + offset.xy * float2(-1, -1);
			float2 uv2 = uv + offset.xy * float2(-1,  1);
			float2 uv3 = uv + offset.xy * float2( 1, -1);
			
		    half4 col = 0;
		    col += SampleBlurTexture(uv);
		    col += SampleBlurTexture(uv0);
		    col += SampleBlurTexture(uv1);
		    col += SampleBlurTexture(uv2);
		    col += SampleBlurTexture(uv3);
			col *= 0.2f;
		    
		    return col;
		}

		//==============================================================================//
		//===================================Gaussian===================================//
		//==============================================================================//
		half4 GaussianBlurFragmentPass(Varyings IN) : SV_Target
		{
			float2 uv = IN.uv;
			float4 offset = GetOffset(uv);
			
			float4 uv01 = uv.xyxy + offset.xyxy * float4(1, 1, -1, -1);
			float4 uv23 = uv.xyxy + offset.xyxy * float4(1, 1, -1, -1) * 2.0;
			float4 uv45 = uv.xyxy + offset.xyxy * float4(1, 1, -1, -1) * 6.0;
			
		    half4 col = 0;
		    col += SampleBlurTexture(uv) * 0.4;
		    col += SampleBlurTexture(uv01.xy) * 0.15;
		    col += SampleBlurTexture(uv01.zw) * 0.15;
		    col += SampleBlurTexture(uv23.xy) * 0.10;
		    col += SampleBlurTexture(uv23.zw) * 0.10;
		    col += SampleBlurTexture(uv45.xy) * 0.05;
		    col += SampleBlurTexture(uv45.zw) * 0.05;
		    
		    return col;
		}
		
		ENDHLSL

		//Pass
		Pass
		{
			Name "Box Blur"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment BoxBlurFragmentPass
			
			ENDHLSL
		}

		Pass
		{
			Name "Gaussian Blur"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment GaussianBlurFragmentPass
			
			ENDHLSL
		}

		
	}
}