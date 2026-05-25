Shader "RoxamiRP/Utils/ProximityColorModifier"
{
	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		HLSLINCLUDE

		#include "Packages/roxamirpcore/Shaders/Core/Common.hlsl"
		#include "Packages/roxamirpcore/Shaders/Core/FullScreenTriangle.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		float4 _PostProximityColor;
		float _PostProximityDistance;
		float _PostProximitySoftness;

		half4 ProximityColorModifierFragmentPass(Varyings IN) : SV_Target
		{
			float depth = SampleSceneDepth(IN.uv);
			float linearDepth = LinearEyeDepth(depth, _ZBufferParams);

			float distanceMin = _PostProximityDistance - _PostProximitySoftness;
			float distanceMax = _PostProximityDistance;

			float alpha = smoothstep(distanceMax, distanceMin, linearDepth);

			return float4(_PostProximityColor.rgb, alpha * _PostProximityColor.a);
		}

		ENDHLSL

		Pass
		{
			Name "Proximity Color Modifier"

			HLSLPROGRAM

			#pragma target 3.5
			#pragma vertex FullScreenTriangle
			#pragma fragment ProximityColorModifierFragmentPass

			ENDHLSL
		}
	}
}
