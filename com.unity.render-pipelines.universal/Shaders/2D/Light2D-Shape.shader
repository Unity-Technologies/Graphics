Shader "Hidden/Light2D-Shape"
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
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;

                SHADOW_COORDS(TEXCOORD1)
                NORMALS_LIGHTING_COORDS(TEXCOORD2, TEXCOORD3)
                LIGHT_OFFSET(TEXCOORD4)
            };

            UNITY_LIGHT2D_DATA

            half _InverseHDREmulationScale;

            TEXTURE2D(_CookieTex);          // This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);

            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES

            Varyings vert(Attributes a)
            {
                Varyings o = (Varyings)0;
                o.lightOffset = a.color;
                PerLight2D light = GetPerLight2D(o.lightOffset);

                float3 positionOS = a.positionOS;

                positionOS.x = positionOS.x + light.FalloffDistance * a.color.r;
                positionOS.y = positionOS.y + light.FalloffDistance * a.color.g;

                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = light.Color * _InverseHDREmulationScale;
                o.color.a = a.color.a;
#if USE_VOLUMETRIC
                o.color.a = light.Color.a  * light.VolumeOpacity;
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

            FragmentOutput frag(Varyings i) : SV_Target
            {
                PerLight2D light = GetPerLight2D(i.lightOffset);
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
            ENDHLSL
        }
    }
}
