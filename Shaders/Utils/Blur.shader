Shader "RoxamiRP/Utils/Blur"
{
	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE

		#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
		#include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"

		//Box: xy: (1 / texelSize) * blurRaios
		//Gaussian: 
		float4 _PostBlurOffset;

		TEXTURE2D(_PostBlurInputTexture);
		SAMPLER(sampler_LinearClamp);

		half4 SampleBlurTexture(float2 uv)
		{
			return SAMPLE_DEPTH_TEXTURE_LOD(_PostBlurInputTexture, sampler_LinearClamp, uv, 0);
		}

		struct BlurVarings
		{
			float4 positionCS	: SV_POSITION;
			float2 uv			: TEXCORRD0;
			float4 uv01			: TEXCOORD1;
			float4 uv23			: TEXCOORD2;
			float4 uv45			: TEXCOORD3;
		};

		//==============================================================================//
		//======================================Box=====================================//
		//==============================================================================//
		BlurVarings BoxBlurVertexPass (uint vertexID : SV_VertexID)
		{
		    BlurVarings output = (BlurVarings) 0;
		    
		    InitializedFullScreenTriangle(vertexID, output.positionCS, output.uv);

			output.uv01.xy = output.uv.xy + _PostBlurOffset.xy * float2( 1,  1);
			output.uv01.zw = output.uv.xy + _PostBlurOffset.xy * float2(-1, -1);
			output.uv23.xy = output.uv.xy + _PostBlurOffset.xy * float2(-1,  1);
			output.uv23.zw = output.uv.xy + _PostBlurOffset.xy * float2( 1, -1);

		    return output;
		}
		
		half4 BoxBlurFragmentPass(BlurVarings IN) : SV_Target
		{
		    half4 col = 0;
		    col += SampleBlurTexture(IN.uv);
		    col += SampleBlurTexture(IN.uv01.xy);
		    col += SampleBlurTexture(IN.uv01.zw);
		    col += SampleBlurTexture(IN.uv23.xy);
		    col += SampleBlurTexture(IN.uv23.zw);
			col *= 0.2f;
		    
		    return col;
		}

		//==============================================================================//
		//===================================Gaussian===================================//
		//==============================================================================//
		BlurVarings GaussianBlurVertexPass (uint vertexID : SV_VertexID)
		{
		    BlurVarings output = (BlurVarings) 0;
		    
		    InitializedFullScreenTriangle(vertexID, output.positionCS, output.uv);

			output.uv01 = output.uv.xyxy + _PostBlurOffset.xyxy * float4(1, 1, -1, -1);
			output.uv23 = output.uv.xyxy + _PostBlurOffset.xyxy * float4(1, 1, -1, -1) * 2.0;
			output.uv45 = output.uv.xyxy + _PostBlurOffset.xyxy * float4(1, 1, -1, -1) * 6.0;

		    return output;
		}

		half4 GaussianBlurFragmentPass(BlurVarings IN) : SV_Target
		{
		    half4 col = 0;
		    col += SampleBlurTexture(IN.uv) * 0.4;
		    col += SampleBlurTexture(IN.uv01.xy) * 0.15;
		    col += SampleBlurTexture(IN.uv01.zw) * 0.15;
		    col += SampleBlurTexture(IN.uv23.xy) * 0.10;
		    col += SampleBlurTexture(IN.uv23.zw) * 0.10;
		    col += SampleBlurTexture(IN.uv45.xy) * 0.05;
		    col += SampleBlurTexture(IN.uv45.zw) * 0.05;
		    
		    return col;
		}
		
		ENDHLSL

		//Pass
		Pass
		{
			Name "Box Blur"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex BoxBlurVertexPass
			#pragma fragment BoxBlurFragmentPass
			
			ENDHLSL
		}

		Pass
		{
			Name "Gaussian Blur"
			
			HLSLPROGRAM
			
			#pragma target 3.5
			#pragma vertex GaussianBlurVertexPass
			#pragma fragment GaussianBlurFragmentPass
			
			ENDHLSL
		}

		
	}
}