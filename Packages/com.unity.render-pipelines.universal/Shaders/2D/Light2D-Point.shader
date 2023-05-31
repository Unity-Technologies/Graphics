Shader "Hidden/Light2D-Point"
{
    Properties
    {
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _HandleZTest ("_HandleZTest", Int) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend [_SrcBlend][_DstBlend]
            ZWrite Off
            ZTest [_HandleZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local USE_POINT_LIGHT_COOKIES __
            #pragma multi_compile_local LIGHT_QUALITY_FAST __
            #pragma multi_compile_local USE_NORMAL_MAP __
            #pragma multi_compile_local USE_ADDITIVE_BLENDING __
            #pragma multi_compile_local USE_VOLUMETRIC __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half2   uv              : TEXCOORD0;
                half2   lookupUV        : TEXCOORD1;  // This is used for light relative direction

                NORMALS_LIGHTING_COORDS(TEXCOORD2, TEXCOORD3)
                SHADOW_COORDS(TEXCOORD4)
                LIGHT_OFFSET(TEXCOORD5)
            };

            UNITY_LIGHT2D_DATA

#if USE_POINT_LIGHT_COOKIES
            TEXTURE2D(_PointLightCookieTex);
            SAMPLER(sampler_PointLightCookieTex);
#endif

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);

            TEXTURE2D(_LightLookup);
            SAMPLER(sampler_LightLookup);
            half4 _LightLookup_TexelSize;

            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES

            half _IsFullSpotlight;
            half _InverseHDREmulationScale;

            Varyings vert(Attributes a)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(a.positionOS);
                output.uv = a.texcoord;
                output.lightOffset = a.color;

#if USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA
                PerLight2D light = GetPerLight2D(output.lightOffset);
#endif

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(a.positionOS);
                worldSpacePos.w = 1;

                float4 lightSpacePos = mul(_L2D_INVMATRIX, worldSpacePos);
                float halfTexelOffset = 0.5 * _LightLookup_TexelSize.x;
                output.lookupUV = 0.5 * (lightSpacePos.xy + 1) + halfTexelOffset;

                TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, _L2D_POSITION.xyz, _L2D_POSITION.w)
                TRANSFER_SHADOWS(output)

                return output;
            }

            FragmentOutput frag(Varyings i)
            {

#if USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA
                PerLight2D light = GetPerLight2D(i.lightOffset);
#endif

                half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

                // Inner Radius
                half attenuation = saturate(_L2D_INNER_RADIUS_MULT * lookupValue.r);   // This is the code to take care of our inner radius

                // Spotlight
                half isFullSpotlight = _L2D_INNER_ANGLE == 1.0f;
                half spotAttenuation = saturate((_L2D_OUTER_ANGLE - lookupValue.g + _IsFullSpotlight) * (1.0f / (_L2D_OUTER_ANGLE - _L2D_INNER_ANGLE)));
                attenuation = attenuation * spotAttenuation;

                half2 mappedUV;
                mappedUV.x = attenuation;
                mappedUV.y = _L2D_FALLOFF_INTENSITY;
                attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

#if USE_POINT_LIGHT_COOKIES
                half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex, i.lookupUV);
                half4 lightColor = cookieColor * _L2D_COLOR;
#else
                half4 lightColor = _L2D_COLOR;
#endif

#if USE_ADDITIVE_BLENDING || USE_VOLUMETRIC
                lightColor *= attenuation;
#else
                lightColor.a = attenuation;
#endif

                APPLY_NORMALS_LIGHTING(i, lightColor, _L2D_POSITION.xyz, _L2D_POSITION.w);
                APPLY_SHADOWS(i, lightColor, _L2D_SHADOW_INTENSITY);

#if USE_VOLUMETRIC
                lightColor *= _L2D_VOLUME_OPACITY;
#endif

                return ToFragmentOutput(lightColor * _InverseHDREmulationScale);
            }
            ENDHLSL
        }
    }
}
