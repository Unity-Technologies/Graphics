Shader "Hidden/HDRP/IntegrateHDRISkyMIS"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ImportanceSampling2D.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            TextureCube<float4> _Cubemap;
            Texture2D<float4>   _Marginal;
            Texture2D<float4>   _ConditionalMarginal;
            uint                _SamplesCount;
            float4              _Sizes;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            // With HDRI that have a large range (including the sun) it can be challenging to
            // compute the lux value without multiple importance sampling.
            // We instead use a brute force Uniforme Spherical integration of the upper hemisphere
            // with a large number of sample. This is fine as this happen in the editor.
            real3 GetUpperHemisphereLuxValue(TEXTURECUBE_PARAM(skybox, sampler_skybox), real3 N, int i)
            {
                //const float coef    = FOUR_PI/float(_SamplesCount);
                //const float coef    = TWO_PI/float(_SamplesCount);
                //float  usedSamples  = 0.0f;
                float3 sum          = 0.0;
                //float  coef         = 4.0f*PI/(float(_SamplesCount));
                //float  coef = 0.5f*PI*PI*_Sizes.z*_Sizes.w;
                //for (uint i = 0; i < _SamplesCount; ++i)
                {
                    float2 xi = Hammersley2d(i, _SamplesCount);
                    float2 latLongUV;
                    float3 sampleDir;
                    float2 info = ImportanceSamplingHemiLatLong(latLongUV, sampleDir, xi, _Sizes.xyz, _Marginal, s_linear_clamp_sampler, _ConditionalMarginal, s_linear_clamp_sampler);

                    sampleDir = normalize(LatlongToDirectionCoordinate(saturate(latLongUV)));

                    float angle = (1.0f - latLongUV.y)*PI;
                    ////float cos0 = cos((latLongUV.y - 0.5)*PI);
                    float cos0 = cos(angle);
                    //float sin0 = sqrt(saturate(1.0f - cos0*cos0));
                    float sin0 = sin(angle);

                    real3 val = SAMPLE_TEXTURECUBE_LOD(skybox, sampler_skybox, sampleDir, 0).rgb;

                    float pdf = info.x;

                    //if (pdf > 1e-10f)
                    //{
                    //    //sum += saturate(cos0)*val*coef/(2.0f*PI);// *(coef * abs(sin0));//pdf);
                    //}
                    if (pdf > 1e-6f)
                        sum = saturate(cos0)*abs(sin0)*val;//pdf;// *(coef * abs(sin0));//pdf);
                }

                return sum/float(_SamplesCount);
                    //;//accumWeight;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Integrate upper hemisphere (Y up)
                float3 N = float3(0.0, 1.0, 0.0);

                int i = floor(input.texCoord.x*_SamplesCount);

                //float3 intensity = GetUpperHemisphereLuxValue(TEXTURECUBE_ARGS(_Cubemap, s_trilinear_clamp_sampler), N);
                float3 intensity = GetUpperHemisphereLuxValue(TEXTURECUBE_ARGS(_Cubemap, s_point_clamp_sampler), N, i);

                //return float4(intensity.rgb, Luminance(intensity));
                return float4(intensity.rgb, max(intensity.r, max(intensity.g, intensity.b)));
            }

            ENDHLSL
        }
    }
    Fallback Off
}
