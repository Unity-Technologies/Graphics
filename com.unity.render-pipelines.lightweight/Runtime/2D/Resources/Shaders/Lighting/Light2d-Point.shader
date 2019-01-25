Shader "Hidden/Light2D-Point"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
    }

	

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
	#pragma multi_compile LIGHT_QUALITY_FAST __

	struct Attributes
	{
		float4 positionOS   : POSITION;
		float2 texcoord     : TEXCOORD0;
	};

	struct Varyings
	{
		float4  positionCS		: SV_POSITION;
		float2  uv				: TEXCOORD0;
		float2	screenUV		: TEXCOORD1;
		float2	lookupUV		: TEXCOORD2;  // This is used for light relative direction
		float2	lookupNoRotUV	: TEXCOORD3;  // This is used for screen relative direction of a light

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

			#pragma multi_compile LIGHT_QUALITY_FAST __

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			TEXTURE2D(_LightLookup);
			SAMPLER(sampler_LightLookup);

			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);

			half4			_LightColor;
			float4			_LightPosition;
			half4x4			_LightInvMatrix;
			half4x4			_LightNoRotInvMatrix;
			half			_LightZDistance;
			half			_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			half			_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			half			_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			half			_InverseLightIntensityScale;

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

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 cookie = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,  input.uv);

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
				//attenuation = attenuation * attenuation;

				// Calculate final color

				#if LIGHT_QUALITY_FAST
					float3 dirToLight = input.lightDirection.xyz;  
				#else
					// This will be the code later for accurate point lights
					float3 dirToLight;
					dirToLight.xy = _LightPosition.xy - input.positionWS.xy; // Calculate this in the vertex shader so we have less precision issues...
					dirToLight.z  = _LightZDistance;
					dirToLight	  = normalize(dirToLight);
				#endif


				float cosAngle = (1 - usingDefaultNormalMap) * saturate(dot(dirToLight, normalUnpacked)) + usingDefaultNormalMap;
				half4 lightColor = _LightColor * attenuation * cosAngle;

				return lightColor * _InverseLightIntensityScale;

				//return 1;
            }
            ENDHLSL
        }
    }
}
