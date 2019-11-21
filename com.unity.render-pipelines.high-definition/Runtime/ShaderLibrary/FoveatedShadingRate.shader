Shader "Hidden/HDRP/FoveatedShadingRate"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma enable_d3d11_debug_symbols
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VariableRateShading.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetQuadVertexPosition(input.vertexID);
        output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
        output.texcoord = GetQuadTexCoord(input.vertexID);
        return output;
    }

    float rand(float2 co)
    {
        return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
    }

    // TODO
    // - fovea parameters center, radius, falloff, etc
    // - adjust constants from CPU side based on UI settings and/or GPU timing (like dynamic resolution)

    float FoveaDensity(float2 uv)
    {
        float2 pos = uv * 2.0f - float2(1.0f, 1.0f);

        pos += float2 (0.1f, 0.2f);
        //pos += 0.5f * float2(_TimeParameters.y, _TimeParameters.z);

        float d = length(pos);

        //return rand(pos + float2(_TimeParameters.y, _TimeParameters.z));

        return (d);
    }

    uint4 Frag(Varyings input) : SV_Target
    {
        float density = FoveaDensity(input.texcoord.xy);

        return VRS_DensityToShadingRate(1.0f - density);
    }

    float4 FragDebug(Varyings input) : SV_Target
    {
        float density = FoveaDensity(input.texcoord.xy);

        uint4 shadingRate = VRS_DensityToShadingRate(density);
        density = VRS_ShadingRateToDensity(shadingRate.x);

        return float4(density, 0, 0, 0);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // Pass 0 : R8 target with 4-bit palette
        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        // Pass 1 : Debug Rendering for pass 0
        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragDebug
            ENDHLSL
        }
    }
    Fallback Off
}
