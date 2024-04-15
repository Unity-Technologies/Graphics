Shader "Hidden/HDRP/WaterFoam"
{
    Properties {}

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    // #pragma enable_d3d11_debug_symbols

    #pragma vertex Vert
    #pragma fragment Frag
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
            // Package includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterDeformer/WaterDeformer.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterDeformationUtilities.hlsl"

            #define SURFACE_FOAM_MUTLIPLIER 2.0
            #define DEEP_FOAM_MUTLIPLIER 5.0

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
                float2 positionWS : DEFORMER_POSITION_WS;
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
                varyings.positionWS = currentDeformer.position.xz * 2 + float2(x, y) * 2;

                // Remap  the position into the normalized area space
                float2 vertexPositionCS = (varyings.positionWS - _FoamRegionOffset * 2) * _FoamRegionScale;

                // Output the clip space position
                varyings.positionHS = float4(vertexPositionCS.x, -vertexPositionCS.y, 0.5, 1.0);
                return varyings;
            }

            float2 Frag(Varyings input) : SV_Target
            {
                // Grab the current deformer
                WaterDeformerData deform = _WaterDeformerData[input.deformerID];

                // Deformer specific code
                float2 foamData = 0.0;
                if (deform.type == WATERDEFORMERTYPE_SHORE_WAVE)
                {
                    // Evaluate the wave data
                    WaveData waveData = EvaluateWaveData(input.deformerPosOS.x - deform.waveOffset, deform.waveSpeed, deform.waveLength);

                    // Wave activation (only on for the waves that are actually displayed)
                    float waveActivation = EvaluateSineWaveActivation(waveData.bellIndex, deform.waveRepetition);

                    // We want the foam to only appear on the tip of the waves
                    float foamHeightThreshold = EvaluateWaveBlendAttenuation(deform, input.normalizedPos);

                    // Define where the foam appears on the wave
                    float surfacefoamWaveLocation = saturate((waveData.position - 0.1) / 0.1) * (1.0 - saturate((waveData.position - 0.5) / 0.02));
                    float deepFoamWaveLocation = saturate((waveData.position - 0.1) / 0.1) * (1.0 - saturate((waveData.position - 0.3) / 0.1));

                    // Define what amount of foam appears
                    float deepfoamAmount = EvaluateDeepFoamAmount(deform, input.normalizedPos.x);
                    float surfaceFoamAmount = EvaluateSurfaceFoamAmount(deform, input.normalizedPos);

                    // Evaluate the perlin noise
                    float perlinNoise = 0.2 + saturate(DeformerNoise2D(input.positionWS * 0.25));

                    // Combine to generate the foam
                    foamData.x = surfaceFoamAmount * surfacefoamWaveLocation;
                    foamData.y = deepfoamAmount * deepFoamWaveLocation;
                    foamData *= perlinNoise * waveActivation * foamHeightThreshold;
                }

                foamData.x *= deform.surfaceFoamDimmer * SURFACE_FOAM_MUTLIPLIER;
                foamData.y *= deform.deepFoamDimmer * DEEP_FOAM_MUTLIPLIER;
                return foamData * _DeltaTime * 2.0;
            }
            ENDHLSL
        }

        Pass
        {
            // This program doesn't require any culling or ztesting
            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            // Package includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterGenerationUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/FoamGenerator/WaterFoamGenerator.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterDeformationUtilities.hlsl"

            #define SURFACE_FOAM_MUTLIPLIER 5.0
            #define DEEP_FOAM_MUTLIPLIER 3.0

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                uint instanceID : INSTANCEID_SEMANTIC;
            };

            struct Varyings
            {
                float4 positionHS : SV_Position;
                float2 positionOS : GENERATOR_POSITION;
                float2 centeredPos : GENERATOR_CENTERED_POSITION;
                float2 normalizedPos : GENERATOR_NORMALIZED_POSITION;
                float2 positionWS : GENERATOR_POSITION_WS;
                int generatorID : GENERATOR_ID;
            };

            Varyings Vert(Attributes input)
            {
                Varyings varyings;
                varyings.generatorID = input.instanceID;

                // Grab the current deformer
                WaterGeneratorData generator = _WaterGeneratorData[varyings.generatorID];

                // Comphte the object space position of the quad
                varyings.positionOS = generatorCorners[input.vertexID] * generator.regionSize * 0.5;
                varyings.centeredPos = generatorCorners[input.vertexID].xy;
                varyings.normalizedPos = varyings.centeredPos * 0.5 + 0.5;

                // Evaluate the world space vertex position (add rotation here)
                float cosRot = cos(generator.rotation);
                float sinRot = sin(generator.rotation);
                float x = varyings.positionOS.x * cosRot - sinRot * varyings.positionOS.y;
                float y = varyings.positionOS.x * sinRot + cosRot * varyings.positionOS.y;
                varyings.positionWS = generator.position.xz * 2 + float2(x, y) * 2;

                // Remap  the position into the normalized area space
                float2 vertexPositionCS = (varyings.positionWS - _FoamRegionOffset * 2) * _FoamRegionScale;

                // Output the clip space position
                varyings.positionHS = float4(vertexPositionCS.x, -vertexPositionCS.y, 0.5, 1.0);
                return varyings;
            }

            float2 Frag(Varyings input) : SV_Target
            {
                // Grab the current deformer
                WaterGeneratorData generator = _WaterGeneratorData[input.generatorID];

                // Evaluate the perlin noise
                float perlinNoise = 0.2 + saturate(DeformerNoise2D(input.positionWS * 0.25));

                // Deformer specific code
                float2 foamData = 0.0;
                if (generator.type == WATERFOAMGENERATORTYPE_DISK)
                {
                    float distanceToCenter = saturate(length(input.centeredPos));
                    foamData = (1.0 - distanceToCenter * distanceToCenter);
                }
                else if (generator.type == WATERFOAMGENERATORTYPE_RECTANGLE)
                {
                    foamData = 1.0;
                }
                else
                {
                    float2 texUV = input.normalizedPos * generator.scaleOffset.xy + generator.scaleOffset.zw;
                    foamData = SAMPLE_TEXTURE2D_LOD(_WaterGeneratorTextureAtlas, s_linear_clamp_sampler, texUV, 0).xy;
                }

                // Apply the multipliers
                foamData.x *= generator.surfaceFoamDimmer * SURFACE_FOAM_MUTLIPLIER * perlinNoise;
                foamData.y *= generator.deepFoamDimmer * DEEP_FOAM_MUTLIPLIER * perlinNoise;

                return foamData * _DeltaTime;
            }
            ENDHLSL
        }

        Pass
        {
            // This program doesn't require any culling or ztesting
            Cull Off
            ZTest Off
            ZWrite Off
            Blend Zero SrcAlpha

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                // Attenuation formula must be in sync with UpdateWaterFoamSimulation in C#
                return float4(0.0, 0.0, 0.0, exp(-_DeltaTime * _FoamPersistenceMultiplier * 0.5));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
