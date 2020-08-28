Shader "Hidden/Light2D-Shape-Volumetric"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local SPRITE_LIGHT __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float4 volumeColor  : TANGENT;

#ifdef SPRITE_LIGHT
                half2  uv           : TEXCOORD0;
#endif
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;

                SHADOW_COORDS(TEXCOORD1)
            };

            half4 _LightColor;
            half  _FalloffDistance;
            half4 _FalloffOffset;
            half  _VolumeOpacity;
            half  _InverseHDREmulationScale;

#ifdef SPRITE_LIGHT
            TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);
#else
            uniform half  _FalloffIntensity;
            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
#endif

            SHADOW_VARIABLES

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                float3 positionOS = attributes.positionOS;
                positionOS.x = positionOS.x + _FalloffDistance * attributes.color.r + (1 - attributes.color.a) * _FalloffOffset.x;
                positionOS.y = positionOS.y + _FalloffDistance * attributes.color.g + (1 - attributes.color.a) * _FalloffOffset.y;


                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = _LightColor * _InverseHDREmulationScale;
                o.color.a = _VolumeOpacity;

#ifdef SPRITE_LIGHT
                o.uv = attributes.uv;
#else
                o.uv = float2(attributes.color.a, _FalloffIntensity);
#endif
                TRANSFER_SHADOWS(o)

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color;

#if SPRITE_LIGHT
                color *= SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
#else
                color.a = i.color.a * SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
#endif

                APPLY_SHADOWS(i, color, _ShadowVolumeIntensity);

                return color;

            }
            ENDHLSL
        }
    }
}
