Shader "Hidden/CubeToPano"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURECUBE(_CubeTexture);
            SAMPLER(sampler_CubeTexture);
            float _CubeMipLevel;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            half2 DirectionToSphericalTexCoordinate(half3 dir_in)      // use this for the lookup
            {
                half3 dir = normalize(dir_in);
                // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
                float recipPi = 1.0 / 3.1415926535897932384626433832795;
                return half2(1.0 - 0.5 * recipPi * atan2(dir.x, -dir.z), asin(dir.y) * recipPi + 0.5);
            }

            half3 SphericalTexCoordinateToDirection(half2 sphTexCoord)
            {
                float pi = 3.1415926535897932384626433832795;
                float theta = (1 - sphTexCoord.x) * (pi * 2);
                float phi = (sphTexCoord.y - 0.5) * pi;

                float csTh, siTh, csPh, siPh;
                sincos(theta, siTh, csTh);
                sincos(phi, siPh, csPh);

                // theta is 0 at negative Z (backwards). Coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
                return float3(siTh * csPh, siPh, -csTh * csPh);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 dir = SphericalTexCoordinateToDirection(input.texcoord.xy);
                return SAMPLE_TEXTURECUBE_LOD(_CubeTexture, sampler_CubeTexture, dir, _CubeMipLevel);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
