Shader "Hidden/Light2d-Point-Volumetric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

	struct Attributes
	{
		float4 positionOS   : POSITION;
		float2 volumeColor  : TANGENT;
	};

	struct Varyings
	{
		float4  positionCS		: SV_POSITION;
		float2	lookupUV		: TEXCOORD2;  // This is used for light relative direction
		float2	lookupNoRotUV	: TEXCOORD3;  // This is used for screen relative direction of a light
	};
	ENDHLSL


	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha One
			BlendOp Add
			ZWrite Off
			ZTest Off 
			Cull Off  // Shape lights have their interiors with the wrong winding order

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_LightLookup);
			SAMPLER(sampler_LightLookup);

			uniform half			_OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
			uniform half			_InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
			uniform half4x4			_LightInvMatrix;
			uniform half4x4			_LightNoRotInvMatrix;
			uniform float4			_LightColor;
			uniform float4			_LightVolumeColor;

			Varyings vert (Attributes attributes)
            {
				Varyings o;
                o.positionCS = TransformObjectToHClip(attributes.positionOS);

				float4 positionWS;
				positionWS.xyz = TransformObjectToWorld(attributes.positionOS);
				positionWS.w = 1;

				float4 lightSpacePos = mul(_LightInvMatrix, positionWS);
				float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, positionWS);
				o.lookupUV = 0.5 * (lightSpacePos.xy + 1);
				o.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
				half4 lookupValueNoRot = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, i.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
				half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

				// Inner Radius
				half  attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius
				attenuation = attenuation * attenuation;

				// Spotlight
				half  spotAttenuation = saturate((_OuterAngle - lookupValue.g)*_InnerAngleMult);
				spotAttenuation = spotAttenuation * spotAttenuation;
				attenuation = attenuation * spotAttenuation;

				half4 col = _LightColor * _LightVolumeColor * attenuation;
	            return col;
            }
            ENDHLSL
        }
    }
}
