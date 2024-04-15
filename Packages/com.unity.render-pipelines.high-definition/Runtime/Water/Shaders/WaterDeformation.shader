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
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterDeformer/WaterDeformer.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterDeformationUtilities.hlsl"

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
        float2 positionWS : DEFORMER_POSITION_WORLD;
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
        float2 vertexPositionCS = (varyings.positionWS - _WaterDeformationCenter * 2) / (_WaterDeformationExtent);

        // Output the clip space position
        varyings.positionHS = float4(vertexPositionCS.x, -vertexPositionCS.y, 0.5, 1.0);
        return varyings;
    }

    float EvaluateDeformerProperties(Varyings input)
    {
        // Grab the current deformer
        WaterDeformerData deform = _WaterDeformerData[input.deformerID];

        // Deformer specific code
        float amplitude = 0.0;
        if (deform.type == WATERDEFORMERTYPE_SPHERE)
        {
            float distanceToCenter = saturate(length(input.centeredPos));
            amplitude = deform.amplitude * (1.0 - distanceToCenter * distanceToCenter);
        }
        else if (deform.type == WATERDEFORMERTYPE_BOX)
        {
            amplitude = deform.amplitude * EvaluateBoxBlendAttenuation(deform.regionSize,
                        input.deformerPosOS, deform.blendRegion, deform.cubicBlend);
        }
        else if (deform.type == WATERDEFORMERTYPE_BOW_WAVE)
        {
                // TODO: Move this into a shared file and refactor (BowWaveUtilities.hlsl)
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
        else if (deform.type == WATERDEFORMERTYPE_SHORE_WAVE)
        {
            // Evaluate the wave data
            WaveData waveData = EvaluateWaveData(input.deformerPosOS.x - deform.waveOffset, deform.waveSpeed, deform.waveLength);

            // Evaluate the round lobe
            float firstShape = ShoreWaveFirstShape(waveData);

            // Evaluate the breaking lobe
            float secondShape = ShoreWaveSecondShape(waveData);

            // Start from the target amplitude
            amplitude = ShoreWaveAmplitudeVariation(input.positionWS) * deform.amplitude;

            // Blend the lobes
            amplitude *= ShoreWaveBlendShapes(firstShape, secondShape, deform.breakingRange, input.normalizedPos.x);

            // Apply the activation function (code that skips some waves)
            amplitude *= EvaluateSineWaveActivation(waveData.bellIndex, deform.waveRepetition);

            // Apply the edge attenuation
            amplitude *= EvaluateWaveBlendAttenuation(deform, input.normalizedPos);
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
            float Frag(Varyings input) : SV_Target
            {
                return EvaluateDeformerProperties(input);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
