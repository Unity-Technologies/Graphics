Shader "Hidden/HDRP/WaterDeformation"
{
    Properties {}

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    // #pragma enable_d3d11_debug_symbols

    #pragma vertex Vert
    #pragma fragment Frag

    // Package includes
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterDeformer/ProceduralWaterDeformer.cs.hlsl"

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // This program doesn't require any culling or ztesting
            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM

            // The set of deformers that should be applied this frame
            StructuredBuffer<WaterDeformerData> _WaterDeformerData;
            Texture2D<float> _WaterDeformerTextureAtlas;

            // This array allows us to convert vertex ID to local position
            static const float2 deformerCorners[6] = {float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, -1), float2(1, 1), float2(-1, 1)};

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                uint instanceID : INSTANCEID_SEMANTIC;
            };

            struct Varyings
            {
                float4 positionHS : SV_Position;
                float2 deformerPosOS : DEFORMER_POSITION;
                float2 centeredPos : DEFORMER_CENTERED_POSITION;
                float2 normalizedPos : DEFORMER_NORMALIZED_POSITION;
                int deformerID : DEFORMER_ID;
            };

            Varyings Vert(Attributes input)
            {
                Varyings varyings;
                varyings.deformerID = input.instanceID;

                // Grab the current deformer
                WaterDeformerData currentDeformer = _WaterDeformerData[varyings.deformerID];

                // Comphte the object space position of the quad
                varyings.deformerPosOS = deformerCorners[input.vertexID] * currentDeformer.regionSize * 0.5;
                varyings.centeredPos = deformerCorners[input.vertexID].xy;
                varyings.normalizedPos = varyings.centeredPos * 0.5 + 0.5;

                // Evaluate the world space vertex position (add rotation here)
                float cosRot = cos(currentDeformer.rotation);
                float sinRot = sin(currentDeformer.rotation);
                float x = varyings.deformerPosOS.x * cosRot - sinRot * varyings.deformerPosOS.y;
                float y = varyings.deformerPosOS.x * sinRot + cosRot * varyings.deformerPosOS.y;
                float2 vertexPositionWS = currentDeformer.position.xz * 2 + float2(x, y) * 2;

                // Remap  the position into the normalized area space
                float2 vertexPositionCS = (vertexPositionWS - _WaterDeformationCenter * 2) / (_WaterDeformationExtent);

                // Output the clip space position
                varyings.positionHS = float4(vertexPositionCS.x, -vertexPositionCS.y, 0.5, 1.0);
                return varyings;
            }

            struct WaveData
            {
                float position;
                int bellIndex;
            };

            WaveData EvaluateWaveData(float position, float waveSpeed, float waveLength)
            {
                WaveData waveData;

                // Compute the overall position (takes into account wave speed and wave length)
                waveData.position = (position - _SimulationTime * waveSpeed) / waveLength;

                // We need to know in which bell we are.
                waveData.bellIndex = waveData.position > 0.0 ? floor(waveData.position) : ceil(-waveData.position);

                // We're only interested in the fractional part of the position
                waveData.position = 1.0 - frac(waveData.position);

                // Return the wave data
                return waveData;
            }

            float EvaluateSineWaveActivation(uint bellIndex, uint waveRepetition)
            {
                // Based on the requested frequency, return the activation function
                return bellIndex % waveRepetition == 0 ? 1.0 : 0.0;
            }

            float EvaluateBoxBlendAttenuation(float2 regionSize, float2 deformerPosOS, float2 blendRegion, int cubicBlend)
            {
                // Apply the edge attenuation
                float2 distanceToEdges = regionSize * 0.5 - abs(deformerPosOS);
                float2 lerpFactor = saturate(distanceToEdges / blendRegion);
                lerpFactor *= cubicBlend ? lerpFactor : 1;
                return min(lerpFactor.x, lerpFactor.y);
            }

            float EvaluateWaveBlendAttenuation(float2 regionBlend, float deformerLength)
            {
                // Apply the edge attenuation
                float leftFactor = saturate((deformerLength) / regionBlend.x);
                float rightFactor = saturate((1.0 - deformerLength) / (1.0 - regionBlend.y));
                return leftFactor * leftFactor * rightFactor * rightFactor;
            }

            // Distance to a parabola by IQ
            // https://iquilezles.org/articles/distfunctions2d/
            float sdParabola(float2 pos, float k )
            {
                pos.x = abs(pos.x);

                float ik = 1.0/k;
                float p = ik*(pos.y - 0.5*ik)/3.0;
                float q = 0.25*ik*ik*pos.x;

                float h = q*q - p*p*p;
                float r = sqrt(abs(h));

                float x = (h>0.0) ? pow(q+r,1.0/3.0) - pow(abs(q-r), 1.0/3.0)*sign(r-q) : 2.0*cos(atan2(r, q)/3.0)*sqrt(p);

                return length(pos-float2(x,k*x*x)) * sign(pos.x-x);
            }

            float Frag(Varyings input) : SV_Target
            {
                // Grab the current deformer
                WaterDeformerData deform = _WaterDeformerData[input.deformerID];

                // Deformer specific code
                float amplitude = 0.0;
                if (deform.type == PROCEDURALWATERDEFORMERTYPE_SPHERE)
                {
                    float distanceToCenter = saturate(length(input.centeredPos));
                    amplitude = deform.amplitude * (1.0 - distanceToCenter * distanceToCenter);
                }
                else if (deform.type == PROCEDURALWATERDEFORMERTYPE_BOX)
                {
                    amplitude = deform.amplitude * EvaluateBoxBlendAttenuation(deform.regionSize,
                                input.deformerPosOS, deform.blendRegion, deform.cubicBlend);
                }
                else if (deform.type == PROCEDURALWATERDEFORMERTYPE_BOW_WAVE)
                {
                    // First we need to remap the coordinates for the function evaluation
                    float2 normPos = input.deformerPosOS / (deform.regionSize * 0.5);
                    float transitionSize = 0.1;

                    normPos.y = normPos.y * 0.5 + 0.5;
                    float2 parabolaPos = normPos;
                    parabolaPos.y -= transitionSize;

                    float dist = sdParabola(parabolaPos, 1);

                    if (dist > 0.0)
                    {
                        float transition = smoothstep(0, 1, saturate(dist / transitionSize));
                        amplitude = lerp(deform.bowWaveElevation, 0.0, transition);
                    }
                    else
                    {
                        float transition = smoothstep(0, 1, saturate(-dist / (transitionSize)));
                        amplitude = lerp(deform.bowWaveElevation, deform.amplitude, transition);
                    }

                    // Apply attenuation so that the tail has less deformation with a cubic profile
                    float lengthAttenuation = (1.0 - saturate(normPos.y));
                    amplitude *= lengthAttenuation * lengthAttenuation;
                }
                else if (deform.type == PROCEDURALWATERDEFORMERTYPE_SINE_WAVE)
                {
                    // Evaluate the wave data
                    WaveData waveData = EvaluateWaveData(input.deformerPosOS.x - deform.waveOffset, deform.waveSpeed, deform.waveLength);

                    // Evaluate the round lobe
                    float firstLobe = 0.5 * sin((waveData.position - 1.25) * 2.0 * PI) + 0.5;

                    // Evaluate the breaking lobe
                    float secondLobe = waveData.position < 0.2 ? waveData.position * waveData.position  * 25.0 : 0.5 * cos(4 * (waveData.position - 0.2)) + 0.5;

                    // Pick which lobe we shall be using
                    float lobePicking = 1.0 - saturate((deform.peakLocation - input.normalizedPos.x) / deform.peakLocation);
                    amplitude = lerp(firstLobe, secondLobe, lobePicking);

                    // Apply the amplitude attenuation
                    amplitude *= input.normalizedPos.x <= deform.peakLocation ? lerp(0.0, 1.0, lobePicking) : 1.0 - saturate((input.normalizedPos.x - deform.peakLocation) / (1.0 - deform.peakLocation));

                    // Apply the activation function (code that skips some bells)
                    amplitude *= EvaluateSineWaveActivation(waveData.bellIndex, deform.waveRepetition) * deform.amplitude;

                    // Apply the edge attenuation
                    amplitude *= EvaluateWaveBlendAttenuation(deform.blendRegion, input.normalizedPos.y);
                }
                else
                {
                    float2 texUV = input.normalizedPos * deform.scaleOffset.xy + deform.scaleOffset.zw;
                    float texData = SAMPLE_TEXTURE2D_LOD(_WaterDeformerTextureAtlas, s_linear_clamp_sampler, texUV, 0);
                    amplitude = lerp(deform.blendRegion.x, deform.blendRegion.y, texData) * deform.amplitude;
                }

                return amplitude;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
