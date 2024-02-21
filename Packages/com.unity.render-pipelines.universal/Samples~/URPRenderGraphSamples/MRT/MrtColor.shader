Shader "Hidden/MrtColor"
{
    Properties
    {
        _ColorTexture("ColorTexture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

            TEXTURE2D_X(_ColorTexture);

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

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }
    

            // MRT shader
            struct FragmentOutput
            {
                half4 dest0 : SV_Target0;
                half4 dest1 : SV_Target1;
                half4 dest2 : SV_Target2;
            };

            FragmentOutput frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D_X(_ColorTexture, sampler_LinearRepeat, input.texcoord); 
                FragmentOutput output;
                output.dest0 = half4(color.r, 0.0, 0.0, 1.0);
                output.dest1 = half4(0.0, color.g, 0.0, 1.0);
                output.dest2 = half4(0.0, 0.0, color.b, 1.0);
                return output;
            }
            ENDHLSL
        }
    }
}
