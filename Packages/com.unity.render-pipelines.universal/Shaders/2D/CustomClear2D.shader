Shader "Hidden/2D/CustomClear"
{
    HLSLINCLUDE

        //#pragma target 4.5
        //#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScaling.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

        float4 _BlitScaleBias;
        half4 _ClearColor[4];

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        void ClearColor(Varyings input,
            out half4 buffer0 : SV_Target0,
            out half4 buffer1 : SV_Target1,
            out half4 buffer2 : SV_Target2,
            out half4 buffer3 : SV_Target3)
        {
            buffer0 = _ClearColor[0];
            buffer1 = _ClearColor[1];
            buffer2 = _ClearColor[2];
            buffer3 = _ClearColor[3];
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        // 0: Clear color, alpha and stencil to zero
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "ClearColor"
            Stencil
            {
                WriteMask 255
                Ref 0
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment ClearColor
            ENDHLSL
        }
    }

    Fallback Off
}
