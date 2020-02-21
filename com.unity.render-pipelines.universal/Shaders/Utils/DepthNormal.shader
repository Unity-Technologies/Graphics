Shader "Hidden/Universal Render Pipeline/DepthNormal"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DepthNormals"

            HLSLPROGRAM

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normal       : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                half4 positionCS    : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normal       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal);

                output.normal = NormalizeNormalPerVertex(normalInput.normalWS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                return output;
            }

            // Encoding/decoding [0..1) floats into 8 bit/channel RG. Note that 1.0 will not be encoded properly.
            inline float2 EncodeFloatRG(float v)
            {
                float2 kEncodeMul = float2(1.0, 255.0);
                float kEncodeBit = 1.0 / 255.0;
                float2 enc = kEncodeMul * v;
                enc = frac(enc);
                enc.x -= enc.y * kEncodeBit;
                return enc;
            }

            // Encoding/decoding view space normals into 2D 0..1 vector
            inline float2 EncodeViewNormalStereo(float3 n)
            {
                float kScale = 1.7777;
                float2 enc;
                enc = n.xy / (n.z + 1);
                enc /= kScale;
                enc = enc * 0.5 + 0.5;
                return enc;
            }

            inline float4 EncodeDepthNormal(float depth, float3 normal)
            {
                float4 enc;
                enc.xy = EncodeViewNormalStereo(normal);
                enc.zw = EncodeFloatRG(depth);
                return enc;
            }

            float4 Fragment(Varyings input) : SV_Target
            { return 1;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float depth = ComputeNormalizedDeviceCoordinatesWithZ(input.positionWS, UNITY_MATRIX_VP).z;
                float3 normal = NormalizeNormalPerPixel(input.normal);
                return EncodeDepthNormal(depth, normal);
                return float4(depth, normal);
            }
            ENDHLSL
        }
    }
}
