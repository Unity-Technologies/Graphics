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
            #pragma multi_compile_local SPRITE_LIGHT __
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
            };

            half    _InverseHDREmulationScale;
            half4   _LightColor;
            half    _FalloffDistance;
#if USE_VOLUMETRIC
            half    _VolumeOpacity;
#endif

#ifdef SPRITE_LIGHT
            TEXTURE2D(_CookieTex);          // This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);
#else
            half    _FalloffIntensity;
            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
#endif
            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                float3 positionOS = attributes.positionOS;

                positionOS.x = positionOS.x + _FalloffDistance * attributes.color.r;
                positionOS.y = positionOS.y + _FalloffDistance * attributes.color.g;

                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = _LightColor * _InverseHDREmulationScale;
                o.color.a = attributes.color.a;
#if USE_VOLUMETRIC
                o.color.a = _LightColor.a  * _VolumeOpacity;
#endif

#ifdef SPRITE_LIGHT
                o.uv = attributes.uv;
#else
                o.uv = float2(attributes.color.a, _FalloffIntensity);
#endif

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(positionOS);
                worldSpacePos.w = 1;
                TRANSFER_NORMALS_LIGHTING(o, worldSpacePos)
                TRANSFER_SHADOWS(o)

                return o;
            }

            FragmentOutput frag(Varyings i) : SV_Target
            {
                half4 color = i.color;
#if SPRITE_LIGHT
                half4 cookie = SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
    #if USE_ADDITIVE_BLENDING
                color *= cookie * cookie.a;
    #else
                color *= cookie;
    #endif
#else
    #if USE_ADDITIVE_BLENDING
                color *= SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #elif USE_VOLUMETRIC
                color.a = i.color.a * SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #else
                color.a = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #endif
#endif

                APPLY_NORMALS_LIGHTING(i, color);

#if USE_VOLUMETRIC
                APPLY_SHADOWS(i, color, _ShadowVolumeIntensity);
#else
                APPLY_SHADOWS(i, color, _ShadowIntensity);
#endif

                return ToFragmentOutput(color);
            }
            ENDHLSL
        }
    }
}
