Shader "Hidden/HDRP/ClearStencilBuffer"
{
    Properties
    {
        [HideInInspector] _StencilMask ("_StencilMask", Int) = 63
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_Position;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        return output;
    }

    #pragma vertex Vert

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Stencil
            {
                Ref       0
                ReadMask  0
                WriteMask [_StencilMask]
                Comp      Always
                Pass      Replace
            }

            Cull      Off
            ZTest     Always
            ZWrite    Off
            ColorMask 0
            Blend     Off

            HLSLPROGRAM
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return 0;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
