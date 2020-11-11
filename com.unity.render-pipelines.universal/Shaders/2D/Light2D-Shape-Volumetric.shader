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
                // extrusionDir & _FalloffDistance;
                float3 nor          : NORMAL;
                float4 color        : COLOR;
                // Used as data for Shape Lights : x FallOffIntensity, y : _VolumeOpacity
                half2  uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;

                SHADOW_COORDS(TEXCOORD1)
            };

            half  _InverseHDREmulationScale;

#ifdef SPRITE_LIGHT
            TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);
#else
            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
#endif

            SHADOW_VARIABLES

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                float3 positionOS = attributes.positionOS;
                positionOS.x = positionOS.x + attributes.nor.z * attributes.nor.x;
                positionOS.y = positionOS.y + attributes.nor.z * attributes.nor.y; 
                
                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = attributes.color * _InverseHDREmulationScale;
                o.color.a = attributes.color.a * attributes.uv.y;

#ifdef SPRITE_LIGHT
                o.uv = attributes.uv;
#else
                o.uv = float2(attributes.color.a, attributes.uv.x);
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
