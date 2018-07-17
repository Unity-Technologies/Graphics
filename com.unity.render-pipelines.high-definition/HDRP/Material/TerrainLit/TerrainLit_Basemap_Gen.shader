Shader "Hidden/HDRenderPipeline/TerrainLit_Basemap_Gen"
{
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }

    SubShader
    {
        Tags { "SplatCount" = "8" }

        HLSLINCLUDE

        #define USE_LEGACY_UNITY_MATRIX_VARIABLES
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../../ShaderVariables.hlsl"
        #include "../Material.hlsl"

        #pragma shader_feature _TERRAIN_8_LAYERS
        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _MASKMAP
        #pragma shader_feature _ _TERRAIN_BLEND_HEIGHT

        #ifdef _MASKMAP
            // Needed because unity tries to match the name of the used textures to samplers. Masks can be used without splats in Metallic pass.
            SAMPLER(sampler_Mask0);
            #define OVERRIDE_SAMPLER_NAME sampler_Mask0
        #endif
        #include "TerrainLitSplatCommon.hlsl"

        struct appdata_t {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        ENDHLSL

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = TransformWorldToHClip(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 albedo;
                float3 normalTS;
                float metallic;
                float ao;
                TerrainSplatBlend(i.texcoord, float3(0, 0, 0), float3(0, 0, 0),
                    albedo.xyz, normalTS, albedo.w, metallic, ao);

                return albedo;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "RG16"
                "Size" = "1/4"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = TransformWorldToHClip(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float2 frag(v2f i) : SV_Target
            {
                float4 albedo;
                float3 normalTS;
                float metallic;
                float ao;
                TerrainSplatBlend(i.texcoord, float3(0, 0, 0), float3(0, 0, 0),
                    albedo.xyz, normalTS, albedo.w, metallic, ao);

                return float2(metallic, ao);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
