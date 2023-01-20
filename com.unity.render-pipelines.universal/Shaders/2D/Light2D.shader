Shader "Hidden/Light2D"
{
    Properties
    {
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _HandleZTest("_HandleZTest", Int) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local USE_NORMAL_MAP __
            #pragma multi_compile_local USE_ADDITIVE_BLENDING __
            #pragma multi_compile_local USE_VOLUMETRIC __
            #pragma multi_compile_local USE_POINT_LIGHT_COOKIES __
            #pragma multi_compile_local LIGHT_QUALITY_FAST __
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
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;
                half2   lookupUV    : TEXCOORD1;  // This is used for light relative direction

                SHADOW_COORDS(TEXCOORD2)
                NORMALS_LIGHTING_COORDS(TEXCOORD3, TEXCOORD4)
                LIGHT_OFFSET(TEXCOORD5)
            };

            TEXTURE2D(_CookieTex);          // This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);

            TEXTURE2D(_LightLookup);
            SAMPLER(sampler_LightLookup);
            half4 _LightLookup_TexelSize;

#if USE_POINT_LIGHT_COOKIES
            TEXTURE2D(_PointLightCookieTex);
            SAMPLER(sampler_PointLightCookieTex);
#endif

            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES
            UNITY_LIGHT2D_DATA

            half _InverseHDREmulationScale;

            Varyings vert_shape(Attributes a, PerLight2D light)
            {
                Varyings o = (Varyings)0;
                float3 positionOS = a.positionOS;

                positionOS.x = positionOS.x + light.FalloffDistance * a.color.r;
                positionOS.y = positionOS.y + light.FalloffDistance * a.color.g;

                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = light.Color * _InverseHDREmulationScale;
                o.color.a = a.color.a;
#if USE_VOLUMETRIC
                o.color.a = light.Color.a * light.VolumeOpacity;
#endif

                // If Sprite use UV.
                o.uv = (light.LightType == 2) ? a.uv : float2(a.color.a, light.FalloffIntensity);

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(positionOS);
                worldSpacePos.w = 1;
                TRANSFER_NORMALS_LIGHTING(o, worldSpacePos, light.Position.xyz, light.Position.w)
                TRANSFER_SHADOWS(o)
                return o;
            }

            Varyings vert_point(Attributes a, PerLight2D light)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(a.positionOS);
                output.uv = a.uv;

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(a.positionOS);
                worldSpacePos.w = 1;

                float4 lightSpacePos = mul(light.InvMatrix, worldSpacePos);
                float halfTexelOffset = 0.5 * _LightLookup_TexelSize.x;
                output.lookupUV = 0.5 * (lightSpacePos.xy + 1) + halfTexelOffset;

                TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, light.Position.xyz, light.Position.w)
                TRANSFER_SHADOWS(output)
                return output;
            }

            Varyings vert(Attributes attributes)
            {

                PerLight2D light = GetPerLight2D(attributes.color);

                switch (light.LightType)
                {
                    case 0:
                    case 1:
                    case 2:
                    {
                        Varyings v = vert_shape(attributes, light);
                        v.lightOffset = attributes.color;
                        return v;
                    }
                    break;
                    case 3:
                    {
                        Varyings v = vert_point(attributes, light);
                        v.lightOffset = attributes.color;
                        return v;
                    }
                }

                Varyings v = (Varyings)0;
                return v;
            }

            FragmentOutput frag_shape(Varyings i, PerLight2D light)
            {
                half4 lightColor = i.color;
                if (light.LightType == 2)
                {
                    half4 cookie = SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
#if USE_ADDITIVE_BLENDING
                    lightColor *= cookie * cookie.a;
#else
                    lightColor *= cookie;
#endif
                }
                else
                {
#if USE_ADDITIVE_BLENDING
                    lightColor *= SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
#elif USE_VOLUMETRIC
                    lightColor.a = i.color.a * SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
#else
                    lightColor.a = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
#endif
                }

                APPLY_NORMALS_LIGHTING(i, lightColor, light.Position.xyz, light.Position.w);
                APPLY_SHADOWS(i, lightColor, light.ShadowIntensity);
                return ToFragmentOutput(lightColor);
            }

            FragmentOutput frag_point(Varyings i, PerLight2D light)
            {
                half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, i.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

                // Inner Radius
                half attenuation = saturate(light.InnerRadiusMult * lookupValue.r);   // This is the code to take care of our inner radius

                // Spotlight
                half isFullSpotlight = light.InnerAngle == 1.0f;
                half spotAttenuation = saturate((light.OuterAngle - lookupValue.g + isFullSpotlight) * (1.0f / (light.OuterAngle - light.InnerAngle)));
                attenuation = attenuation * spotAttenuation;

                half2 mappedUV;
                mappedUV.x = attenuation;
                mappedUV.y = light.FalloffIntensity;
                attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

                half4 lightColor = light.Color;
#if USE_POINT_LIGHT_COOKIES
                half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex, i.lookupUV);
                lightColor = cookieColor * light.Color;
#endif

#if USE_ADDITIVE_BLENDING || USE_VOLUMETRIC
                lightColor *= attenuation;
#else
                lightColor.a = attenuation;
#endif

                APPLY_NORMALS_LIGHTING(i, lightColor, light.Position.xyz, light.Position.w);
                APPLY_SHADOWS(i, lightColor, light.ShadowIntensity);

#if USE_VOLUMETRIC
                lightColor *= light.VolumeOpacity;
#endif

                return ToFragmentOutput(lightColor * _InverseHDREmulationScale);
            }

            FragmentOutput frag(Varyings i) : SV_Target
            {
                PerLight2D light = GetPerLight2D(i.lightOffset);
                switch (light.LightType)
                {
                    case 0:
                    case 1:
                    case 2:
                    {
                        FragmentOutput output = frag_shape(i, light);
                        return output;
                    }
                    break;
                    case 3:
                    {
                        FragmentOutput output = frag_point(i, light);
                        return output;
                    }
                }

                half4 color = i.color;
                return ToFragmentOutput(color);
            }
            ENDHLSL
        }
    }
}
