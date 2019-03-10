Shader "Hidden/Light2d-Point-Volumetric"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
    }

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

	struct Attributes
	{
		float4 positionOS   : POSITION;
		float2 texcoord     : TEXCOORD0;
		float4  volumeColor		: TANGENT;
	};

	struct Varyings
	{
		float4  positionCS		: SV_POSITION;
		float2  uv				: TEXCOORD0;
		float2	screenUV		: TEXCOORD1;
		float2	lookupUV		: TEXCOORD2;  // This is used for light relative direction
		float2	lookupNoRotUV	: TEXCOORD3;  // This is used for screen relative direction of a light
		float4  volumeColor		: TANGENT;

		#if LIGHT_QUALITY_FAST
			float4	lightDirection	: TEXCOORD4;
		#else
			float4	positionWS : TEXCOORD4;
		#endif
	};
	ENDHLSL


    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend One One
		BlendOp Add
		ZWrite Off
		Cull Off

        Pass
        {
            HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex vert
			#pragma fragment frag
			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile USE_POINT_LIGHT_COOKIES __ 

			#if USE_POINT_LIGHT_COOKIES
			TEXTURE2D(_PointLightCookieTex);
			SAMPLER(sampler_PointLightCookieTex);
			#endif

			TEXTURE2D(_FalloffLookup);
			SAMPLER(sampler_FalloffLookup);
			uniform float _FalloffCurve;

			TEXTURE2D(_LightLookup);
			SAMPLER(sampler_LightLookup);

			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);

			uniform half4	_LightColor;
			uniform float4	_LightPosition;
			uniform half4x4	_LightInvMatrix;
			uniform half4x4	_LightNoRotInvMatrix;
			uniform half	_LightZDistance;
			uniform half	_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half	_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half	_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			uniform half	_InverseLightIntensityScale;

			Varyings vert (Attributes input)
            {
				Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.texcoord;

				float4 worldSpacePos;
				worldSpacePos.xyz = TransformObjectToWorld(input.positionOS.xyz);
				worldSpacePos.w = 1;

				float4 lightSpacePos = mul(_LightInvMatrix, worldSpacePos);
				float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, worldSpacePos);
				output.lookupUV = 0.5 * (lightSpacePos.xy + 1);
				output.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1);

				#if LIGHT_QUALITY_FAST
					output.lightDirection.xy = _LightPosition.xy - worldSpacePos.xy;
					output.lightDirection.z   = _LightZDistance;
					output.lightDirection.w   = 0;
					output.lightDirection.xyz = normalize(output.lightDirection.xyz);
				#else
					output.positionWS = worldSpacePos;
				#endif

				float4 clipVertex = output.positionCS / output.positionCS.w;
				output.screenUV = ComputeScreenPos(clipVertex).xy;
				output.volumeColor = input.volumeColor;

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
				#if USE_POINT_LIGHT_COOKIES
				half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex,  input.lookupUV);
				#endif

				half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);
				half4 lookupValueNoRot = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
				half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

				float usingDefaultNormalMap = (normal.x + normal.y + normal.z) == 0;  // 1 if using a black normal map, 0 if using a custom normal map
				float3 normalUnpacked = UnpackNormal(normal);

				// Inner Radius
				half attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius

				// Spotlight
				half  spotAttenuation = saturate((_OuterAngle - lookupValue.g)*_InnerAngleMult);
				attenuation = attenuation * spotAttenuation;

				half2 mappedUV;
				mappedUV.x = attenuation;
				mappedUV.y = _FalloffCurve;
				attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;


				#if USE_POINT_LIGHT_COOKIES
					half4 lightColor = cookieColor * _LightColor * attenuation;
				#else
					half4 lightColor = _LightColor * attenuation;
				#endif

				return input.volumeColor.a * lightColor * _InverseLightIntensityScale;
            }
            ENDHLSL
        }
    }
}
