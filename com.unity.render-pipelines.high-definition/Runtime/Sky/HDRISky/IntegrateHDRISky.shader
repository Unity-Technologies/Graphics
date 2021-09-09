Shader "Hidden/HDRP/IntegrateHDRI"
{
    Properties
    {
        [HideInInspector]
        _Cubemap ("", CUBE) = "white" {}
    }

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
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

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
                float3 sum = 0.0;
                const float dphi    = 0.005;
                const float dtheta  = 0.005;
                const float coef    = dphi*dtheta;
                for (float phi = 0; phi < 2.0 * PI; phi += dphi)
                {
                    for (float theta = 0; theta < PI / 2.0; theta += dtheta)
                    {
                        // SphericalToCartesian function is for Z up, lets move to Y up with TransformGLtoDX
                        float3 L = TransformGLtoDX(SphericalToCartesian(phi, cos(theta)));
                        real3 val = SAMPLE_TEXTURECUBE_LOD(skybox, sampler_skybox, L, 0).rgb;
                        sum += (cos(theta)*sin(theta)*coef)*val;
                    }
                }

                return sum;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Integrate upper hemisphere (Y up)
                float3 N = float3(0.0, 1.0, 0.0);

                float3 intensity = GetUpperHemisphereLuxValue(TEXTURECUBE_ARGS(_Cubemap, s_trilinear_clamp_sampler), N);

                return float4(intensity.rgb, Luminance(intensity));
            }

            ENDHLSL
        }
    }
    Fallback Off
}
