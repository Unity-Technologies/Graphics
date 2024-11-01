Shader "Hidden/HDRP/WaterDecal"
{
    Properties {}

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    //#pragma enable_d3d11_debug_symbols

    #pragma vertex Vert
    #pragma fragment Frag

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

    StructuredBuffer<WaterDecalData> _WaterDecalData;
    Texture2D<float4> _WaterDecalAtlas;

    float2 TransformUV(float2 transformedPositionAWS)
    {
        return (transformedPositionAWS - _DecalRegionOffset) * _DecalRegionScale;
    }

    struct Attributes
    {
        uint vertexID : VERTEXID_SEMANTIC;
        uint instanceID : INSTANCEID_SEMANTIC;
    };

    struct Varyings
    {
        float4 positionCS : SV_Position;
        float2 uv : TEXCOORD0;
        float2 data : ADDITIONAL_DATA;
        float4 scaleOffset : UV_SCALE_OFFSET;
    };

    Varyings GetDecalVaryings(Attributes input, float4 uvScaleOffset, float2 additionalData = 0)
    {
        Varyings varyings;

        // Grab the current deformer
        WaterDecalData decal = _WaterDecalData[input.instanceID];

        // Compute the object space position of the quad
        float2 uv = GetQuadTexCoord(input.vertexID).yx;
        float2 positionOS = (uv - 0.5) * decal.regionSize;

        // Evaluate the world space vertex position
        float cosRot = decal.forwardXZ.x;
        float sinRot = decal.forwardXZ.y;
        float x = positionOS.x * cosRot - sinRot * positionOS.y;
        float y = positionOS.x * sinRot + cosRot * positionOS.y;
        float2 positionWS = decal.positionXZ + float2(x, y);

        // Remap the position into the normalized area space
        float2 vertexPositionCS = TransformUV(positionWS) * 2.0f;
        varyings.positionCS = float4(vertexPositionCS.x, -vertexPositionCS.y, 0.5, 1.0);

        varyings.uv = uv;
        varyings.scaleOffset = uvScaleOffset;
        varyings.data = additionalData;

        if (uvScaleOffset.x < 0)
            varyings.positionCS.w = FLT_NAN;

        return varyings;
    }

    float2 RemapUV(float2 uv, float4 scaleOffset)
    {
        // Remap UV in atlas - clamp to avoid edge bleeding
        float halfPixel = _DecalAtlasScale * 0.5f;
        return clamp(uv * scaleOffset.xy, halfPixel, scaleOffset.xy - halfPixel) + scaleOffset.zw;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "DeformationDecal"

            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            Varyings Vert(Attributes input)
            {
                WaterDecalData decal = _WaterDecalData[input.instanceID];
                return GetDecalVaryings(input, decal.deformScaleOffset, decal.amplitude);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = RemapUV(input.uv, input.scaleOffset);
                return SAMPLE_TEXTURE2D_LOD(_WaterDecalAtlas, s_linear_clamp_sampler, uv, 0) * input.data.x;
            }
            ENDHLSL
        }

        Pass
        {
            Name "FoamDecal"

            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            Varyings Vert(Attributes input)
            {
                WaterDecalData decal = _WaterDecalData[input.instanceID];
                return GetDecalVaryings(input, decal.foamScaleOffset, float2(decal.surfaceFoamDimmer, decal.deepFoamDimmer));
            }

            float2 Frag(Varyings input) : SV_Target
            {
                float2 uv = RemapUV(input.uv, input.scaleOffset);
                return SAMPLE_TEXTURE2D_LOD(_WaterDecalAtlas, s_linear_clamp_sampler, uv, 0).yz * input.data * _DeltaTime;
            }
            ENDHLSL
        }

        Pass
        {
            Name "MaskDecal"

            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One, One One
            BlendOp Min

            HLSLPROGRAM
            Varyings Vert(Attributes input)
            {
                WaterDecalData decal = _WaterDecalData[input.instanceID];
                return GetDecalVaryings(input, decal.maskScaleOffset);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = RemapUV(input.uv, input.scaleOffset);
                return SAMPLE_TEXTURE2D_LOD(_WaterDecalAtlas, s_linear_clamp_sampler, uv, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "LargeCurrentDecal"

            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            Varyings Vert(Attributes input)
            {
                WaterDecalData decal = _WaterDecalData[input.instanceID];
                return GetDecalVaryings(input, decal.largeCurrentScaleOffset);
            }

            float3 Frag(Varyings input) : SV_Target
            {
                float2 uv = RemapUV(input.uv, input.scaleOffset);
                return SAMPLE_TEXTURE2D_LOD(_WaterDecalAtlas, s_linear_clamp_sampler, uv, 0).xyz;
            }
            ENDHLSL
        }

        Pass
        {
            Name "RipplesCurrentDecal"

            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            Varyings Vert(Attributes input)
            {
                WaterDecalData decal = _WaterDecalData[input.instanceID];
                return GetDecalVaryings(input, decal.ripplesCurrentScaleOffset);
            }

            float3 Frag(Varyings input) : SV_Target
            {
                float2 uv = RemapUV(input.uv, input.scaleOffset);
                return SAMPLE_TEXTURE2D_LOD(_WaterDecalAtlas, s_linear_clamp_sampler, uv, 0).xyz;
            }
            ENDHLSL
        }

        Pass
        {
            Name "FoamAttenuation"

            Cull Off
            ZTest Off
            ZWrite Off
            Blend Zero SrcAlpha

            HLSLPROGRAM
            struct AttenuationAttributes
            {
                uint vertexID : SV_VertexID;
            };

            struct AttenuationVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            AttenuationVaryings Vert(AttenuationAttributes input)
            {
                AttenuationVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }
            float4 Frag(AttenuationVaryings input) : SV_Target
            {
                // Attenuation formula must be in sync with UpdateWaterDecals in C#
                return float4(0.0, 0.0, 0.0, exp(-_DeltaTime * _FoamPersistenceMultiplier * 0.5));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
