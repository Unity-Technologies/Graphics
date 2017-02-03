Shader "Hidden/HDRenderPipeline/CombineSubsurfaceScattering"
{
    Properties
    {
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

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  One [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ FILTER_HORIZONTAL

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Common.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            #define N_PROFILES 8
            #define N_SAMPLES  7

            float4   _FilterKernels[N_PROFILES][N_SAMPLES + 1]; // RGB = weights, A = radial distance
            float4x4 _InvProjMatrix;

            TEXTURE2D_FLOAT(_CameraDepthTexture);
            TEXTURE2D(_GBufferTexture2);
            TEXTURE2D(_IrradianceSource);

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
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float3 Frag(Varyings input) : SV_Target
            {
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);

                float2 gBufferData  = LOAD_TEXTURE2D(_GBufferTexture2, posInput.unPositionSS).ra;
                int    profileID    = int(gBufferData.y * N_PROFILES);
                float  distScale    = gBufferData.x * 0.01;
                float  invDistScale = rcp(distScale);

                // Reconstruct the view-space position.
                float  rawDepth    = LOAD_TEXTURE2D(_CameraDepthTexture, posInput.unPositionSS).r;
                float3 centerPosVS = ComputeViewSpacePosition(posInput.positionSS, rawDepth, _InvProjMatrix);

                // Compute the dimensions of the surface fragment viewed as a quad facing the camera.
                float fragWidth  = ddx(centerPosVS.x);
                float fragheight = ddy(centerPosVS.y);
                float stepSizeX  = rcp(fragWidth);
                float stepSizeY  = rcp(fragheight);

                // Compute the filtering direction.
            #ifdef FILTER_HORIZONTAL
                float  stepSize      = stepSizeX;
                float2 unitDirection = float2(1, 0);
            #else
                float  stepSize      = stepSizeY;
                float2 unitDirection = float2(0, 1);
            #endif
                float2 scaledDirection = distScale * stepSize * unitDirection;

                // Load (1 / (2 * Variance)) for bilateral weighting.
            #ifdef RBG_BILATERAL_WEIGHTS
                float3 halfRcpVariance = _FilterKernels[profileID][N_SAMPLES].rgb;
            #else
                float  halfRcpVariance = _FilterKernels[profileID][N_SAMPLES].a;
            #endif
                // Take the first (central) sample.
                float2 samplePosition   = posInput.unPositionSS;
                float3 sampleWeight     = _FilterKernels[profileID][0].rgb;
                float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                // Accumulate filtered irradiance (already weighted by (albedo / Pi)).
                float3 totalIrradiance = sampleIrradiance * sampleWeight;

                // Make sure bilateral filtering does not cause energy loss.
                // TODO: ask Morten if there is a better way to do this.
                float3 totalWeight = sampleWeight;

                [unroll]
                for (int i = 1; i < N_SAMPLES; i++)
                {
                    samplePosition = posInput.unPositionSS + scaledDirection * _FilterKernels[profileID][i].a;
                    sampleWeight   = _FilterKernels[profileID][i].rgb;

                    rawDepth         = LOAD_TEXTURE2D(_CameraDepthTexture, samplePosition).r;
                    sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource,   samplePosition).rgb;

                    // Apply bilateral weighting.
                    // Ref #1: Skin Rendering by Pseudo–Separable Cross Bilateral Filtering.
                    // Ref #2: Separable SSS, Supplementary Materials, Section E.
                    float sampleDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float zDistance   = invDistScale * sampleDepth - (invDistScale * centerPosVS.z);
                    sampleWeight     *= exp(-zDistance * zDistance * halfRcpVariance);

                    totalIrradiance += sampleIrradiance * sampleWeight;
                    totalWeight     += sampleWeight;
                }

                return totalIrradiance / totalWeight;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
