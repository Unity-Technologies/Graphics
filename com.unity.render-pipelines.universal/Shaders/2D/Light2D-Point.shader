Shader "Hidden/Light2D-Point"
{
    Properties
    {
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend [_SrcBlend][_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local USE_POINT_LIGHT_COOKIES __
            #pragma multi_compile_local LIGHT_QUALITY_FAST __
            #pragma multi_compile_local USE_NORMAL_MAP __
            #pragma multi_compile_local USE_ADDITIVE_BLENDING __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half2   uv              : TEXCOORD0;
                half2	lookupUV        : TEXCOORD2;  // This is used for light relative direction
                half2	lookupNoRotUV   : TEXCOORD3;  // This is used for screen relative direction of a light

                NORMALS_LIGHTING_COORDS(TEXCOORD4, TEXCOORD5)
                SHADOW_COORDS(TEXCOORD6)
            };

#if USE_POINT_LIGHT_COOKIES
            TEXTURE2D(_PointLightCookieTex);
            SAMPLER(sampler_PointLightCookieTex);
#endif

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
            half _FalloffIntensity;

            TEXTURE2D(_LightLookup);
            SAMPLER(sampler_LightLookup);
            half4 _LightLookup_TexelSize;

            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES

            half4	    _LightColor;
            float4x4    _LightInvMatrix;
            float4x4    _LightNoRotInvMatrix;
            half	    _OuterAngle;			    // 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
            half	    _InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
            half	    _InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
            half	    _InverseHDREmulationScale;
            half        _IsFullSpotlight;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.texcoord;

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(input.positionOS);
                worldSpacePos.w = 1;

                float4 lightSpacePos = mul(_LightInvMatrix, worldSpacePos);
                float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, worldSpacePos);
                float halfTexelOffset = 0.5 * _LightLookup_TexelSize.x;
                output.lookupUV = 0.5 * (lightSpacePos.xy + 1) + halfTexelOffset;
                output.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1) + halfTexelOffset;

                TRANSFER_NORMALS_LIGHTING(output, worldSpacePos)
                TRANSFER_SHADOWS(output)

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 lookupValueNoRot = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
                half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction


                // Inner Radius
                half attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius

                // Spotlight
                half  spotAttenuation = saturate((_OuterAngle - lookupValue.g + _IsFullSpotlight) * _InnerAngleMult);
                attenuation = attenuation * spotAttenuation;

                half2 mappedUV;
                mappedUV.x = attenuation;
                mappedUV.y = _FalloffIntensity;
                attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

#if USE_POINT_LIGHT_COOKIES
                half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex, input.lookupUV);
                half4 lightColor = cookieColor * _LightColor;
#else
                half4 lightColor = _LightColor;
#endif

#if USE_ADDITIVE_BLENDING
                lightColor *= attenuation;
#else
                lightColor.a = attenuation;
#endif

                APPLY_NORMALS_LIGHTING(input, lightColor);
                APPLY_SHADOWS(input, lightColor, _ShadowIntensity);

                return lightColor * _InverseHDREmulationScale;
            }
            ENDHLSL
        }
    }
}
