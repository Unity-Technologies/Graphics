Shader "Hidden/HDRP/TerrainLit_Basemap_Gen"
{
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }

    SubShader
    {
        Tags { "SplatCount" = "8" }

        HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #define USE_LEGACY_UNITY_MATRIX_VARIABLES
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

        // Terrain builtin keywords
        #pragma shader_feature _TERRAIN_8_LAYERS
        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _MASKMAP

        #pragma shader_feature _TERRAIN_BLEND_HEIGHT

        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.ShaderVariables.hlsl"

        CBUFFER_START(UnityTerrain)
            UNITY_TERRAIN_CB_VARS
            float4 _Control0_ST;
            float4 _Control0_TexelSize;
        CBUFFER_END

        #ifdef _MASKMAP
            // Needed because unity tries to match the name of the used textures to samplers. Masks can be used without splats in Metallic pass.
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
        #endif

        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

        struct Attributes {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texcoord : TEXCOORD0;
        };

        float2 ComputeControlUV(float2 uv)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            return (uv * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
        }

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

            #pragma vertex Vert
            #pragma fragment Frag

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(input.vertex);
                output.texcoord.xy = TRANSFORM_TEX(input.texcoord, _Control0);
                output.texcoord.zw = ComputeControlUV(output.texcoord.xy);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 albedo;
                float3 normalTS;
                float metallic;
                float ao;
                TerrainSplatBlend(input.texcoord.zw, input.texcoord.xy, float3(0, 0, 0), float3(0, 0, 0),
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

            #pragma vertex Vert
            #pragma fragment Frag

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(input.vertex);
                output.texcoord.xy = TRANSFORM_TEX(input.texcoord, _Control0);
                output.texcoord.zw = ComputeControlUV(output.texcoord.xy);
                return output;
            }

            float2 Frag(Varyings input) : SV_Target
            {
                float4 albedo;
                float3 normalTS;
                float metallic;
                float ao;
                TerrainSplatBlend(input.texcoord.zw, input.texcoord.xy, float3(0, 0, 0), float3(0, 0, 0),
                    albedo.xyz, normalTS, albedo.w, metallic, ao);

                return float2(metallic, ao);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
