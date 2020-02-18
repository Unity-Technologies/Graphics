Shader "Hidden/HDRP/IntegrateHDRISkyMIS"
{
    //Properties
    //{
    //    [HideInInspector]
    //    _Cubemap ("", CUBE) = "white" {}
    //}

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
            real3 GetUpperHemisphereLuxValue(TEXTURECUBE_PARAM(skybox, sampler_skybox), real3 N)
            {
                //const float coef    = FOUR_PI/float(_SamplesCount);
                //const float coef    = TWO_PI/float(_SamplesCount);
                float usedSamples = 0.0f;
                float3 sum = 0.0;
                for (uint i = 0; i < _SamplesCount; ++i)
                {
                    float2 xi;// = Hammersley2d(i, _SamplesCount);
                    //xi.y = xi.y*0.5f + 0.5f;
                    //xi.y *= 0.5f;
                    float2 latLongUV;
                    float3 sampleDir;
                    ImportanceSamplingLatLong(latLongUV, sampleDir, xi, _Marginal, s_linear_clamp_sampler, _ConditionalMarginal, s_linear_clamp_sampler);

                    //xi.y = (xi.y - 0.5f)*0.5f + 0.25f + 0.5f;

                    //sampleDir = normalize(LatlongToDirectionCoordinate(saturate(xi)));
                    sampleDir = normalize(LatlongToDirectionCoordinate(saturate(latLongUV)));

                    //float3 L = TransformGLtoDX(sampleDir);

                    float cos0 = saturate( sampleDir.y );
                    float sin0 = sqrt( saturate( 1.0f - cos0*cos0 ) );

                    //if (cos0 > 0.0f)
                    //    usedSamples += 1.0f;

                    //float3 L = TransformGLtoDX(SphericalToCartesian(phi, cos0));
                    //float3 L = TransformGLtoDX(sampleDir);
                    //float3 L = sampleDir;
                    real3 val = SAMPLE_TEXTURECUBE_LOD(skybox, sampler_skybox, sampleDir, 0).rgb;
                    //real3 val = real3(1, 1, 1);
                    //sum += (cos0*sin0*coef)*val;
                    //sum += (cos0*sin0)*val;
                    //sum += val;
                    sum += cos0*sin0*val/max(val.r, max(val.g, val.b));
                }

                //return sum*TWO_PI/usedSamples;
                return sum*(TWO_PI/float(_SamplesCount));
                //return sum/usedSamples;
                //return TWO_PI*sum/float(_SamplesCount);
                //return TWO_PI*sum/float(_SamplesCount);
                //return sum*TWO_PI/usedSamples;
                //return sum/usedSamples;
                //return sum/float(_SamplesCount);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Integrate upper hemisphere (Y up)
                float3 N = float3(0.0, 1.0, 0.0);

                //float3 intensity = GetUpperHemisphereLuxValue(TEXTURECUBE_ARGS(_Cubemap, s_trilinear_clamp_sampler), N);
                float3 intensity = GetUpperHemisphereLuxValue(TEXTURECUBE_ARGS(_Cubemap, s_point_clamp_sampler), N);

                //return float4(intensity.rgb, Luminance(intensity));
                return float4(intensity.rgb, max(intensity.r, max(intensity.g, intensity.b)));
            }

            ENDHLSL
        }
    }
    Fallback Off
}
