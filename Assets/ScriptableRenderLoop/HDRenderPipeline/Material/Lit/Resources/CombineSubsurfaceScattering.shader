Shader "Hidden/HDRenderPipeline/CombineSubsurfaceScattering"
{
    Properties
    {
        _FilterRadius("", Float) = 20
        _BilateralScale("", Float) = 0.1
        [HideInInspector] _DstBlend("", Float) = 1 // Can be set to 1 for blending with specular
    }

    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref  1 // StencilBits.SSS
                Comp Equal
                Pass Keep
            }

            ZTest  Always
            ZWrite Off
            Blend  One [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Common.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            #define N_SAMPLES 7

            float  _BilateralScale;   // Uses world-space units
            float  _DistToProjWindow; // The height of the projection window is 2 meters
            float  _FilterHorizontal; // Vertical = 0, horizontal = 1
            float4 _FilterKernel[7];  // RGB = weights, A = radial distance
            float  _FilterRadius;     // Uses world-space units

            TEXTURE2D(_CameraDepthTexture);
            TEXTURE2D(_IrradianceSource);
            SAMPLER2D(sampler_IrradianceSource);

            #define bilinearSampler sampler_IrradianceSource

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                uint vertexId : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // Generate a triangle in homogeneous clip space, s.t.
                // v0 = (-1, -1, 1), v1 = (3, -1, 1), v2 = (-1, 3, 1).
                float2 uv = float2((input.vertexId << 1) & 2, input.vertexId & 2);
                output.positionCS = float4(uv * 2 - 1, 1, 1);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);

                float rawDepth    = LOAD_TEXTURE2D(_CameraDepthTexture, posInput.unPositionSS).r;
                float cDepth      = LinearEyeDepth(rawDepth, _ZBufferParams);
                float radiusScale = _FilterRadius * _DistToProjWindow / cDepth;

                // Compute the filtering direction.
                float2 unitDirection   = _FilterHorizontal ? float2(1, 0) : float2(0, 1);
                float2 scaledDirection = radiusScale * unitDirection;

                // Premultiply with the inverse of the screen size.
                scaledDirection *= _ScreenSize.zw;

                // Take the first (central) sample.
                float3 sWeight   = _FilterKernel[0].rgb;
                float2 sPosition = posInput.unPositionSS;

                float3 sIrradiance = LOAD_TEXTURE2D(_IrradianceSource, sPosition).rgb;
                float3 cIrradiance = sIrradiance;

                // Accumulate filtered irradiance (already weighted by (albedo / Pi)).
                float3 filteredIrradiance = sIrradiance * sWeight;

                [unroll]
                for (int i = 1; i < N_SAMPLES; i++)
                {
                    sWeight   = _FilterKernel[i].rgb; // TODO: normalize weights
                    sPosition = posInput.positionSS + scaledDirection * _FilterKernel[i].a;

                    sIrradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceSource,   bilinearSampler, sPosition, 0).rgb;
                    rawDepth    = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, bilinearSampler, sPosition, 0).r;

                    // Apply bilateral filtering.
                    float sDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float dDepth = abs(sDepth - cDepth);
                    float dScale = _FilterRadius * _DistToProjWindow * _BilateralScale;
                    float t      = saturate(dScale * dDepth);

                    // TODO: use real-world distances for weighting.
                    filteredIrradiance += lerp(sIrradiance, cIrradiance, t) * sWeight;
                }

                return float4(false ? cIrradiance : filteredIrradiance, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
