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
            Blend  One [_DstBlend], Zero [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

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

            TEXTURE2D(_CameraDepthTexture);
            TEXTURE2D(_GBufferTexture2);
            TEXTURE2D(_IrradianceSource);
            SAMPLER2D(sampler_IrradianceSource);

            #define bilinearSampler sampler_IrradianceSource

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullscreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);

                float rawDepth     = LOAD_TEXTURE2D(_CameraDepthTexture, posInput.unPositionSS).r;
                float centerDepth  = LinearEyeDepth(rawDepth, _ZBufferParams);
                float radiusScale  = LOAD_TEXTURE2D(_GBufferTexture2, posInput.unPositionSS).r;
                float filterRadius = radiusScale * _DistToProjWindow / centerDepth;

                // Compute the filtering direction.
                float x, y;
                sincos(PI / 3, y, x);
                float2 unitDirection   = _FilterHorizontal ? float2(x, y) : float2(-y, x);
                float2 scaledDirection = filterRadius * unitDirection;

                // Premultiply with the inverse of the screen size.
                scaledDirection *= _ScreenSize.zw;

                // Take the first (central) sample.
                float3 sampleWeight   = _FilterKernel[0].rgb;
                float2 samplePosition = posInput.unPositionSS;

                float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;
                float3 centerIrradiance = sampleIrradiance;

                // Accumulate filtered irradiance (already weighted by (albedo / Pi)).
                float3 filteredIrradiance = sampleIrradiance * sampleWeight;

                [unroll]
                for (int i = 1; i < N_SAMPLES; i++)
                {
                    sampleWeight   = _FilterKernel[i].rgb;
                    samplePosition = posInput.positionSS + scaledDirection * _FilterKernel[i].a;

                    sampleIrradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceSource,   bilinearSampler, samplePosition, 0).rgb;
                    rawDepth         = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, bilinearSampler, samplePosition, 0).r;

                    // Apply bilateral filtering.
                    float sampleDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float depthDiff   = abs(sampleDepth - centerDepth);
                    float scaleDiff   = radiusScale * _DistToProjWindow * _BilateralScale;
                    float t           = saturate(depthDiff / scaleDiff);

                    // TODO: use real-world distances for weighting.
                    filteredIrradiance += lerp(sampleIrradiance, centerIrradiance, t) * sampleWeight;
                }

                return float4(filteredIrradiance, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
