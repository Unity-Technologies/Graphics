#ifndef UNDER_WATER_UTILITIES_H
#define UNDER_WATER_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDStencilUsage.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

StructuredBuffer<float> _WaterCameraHeightBuffer;

float GetWaterCameraHeight()
{
    return _WaterCameraHeightBuffer[unity_StereoEyeIndex];
}

StructuredBuffer<uint> _WaterLine;

float GetUnderWaterDistance(uint2 coord)
{
    float2 upVector = float2(_UpDirectionX, _UpDirectionY);
    float2 rightVector = float2(_UpDirectionY, -_UpDirectionX);

    // Find index to sample in 1D buffer
    uint xr = unity_StereoEyeIndex * _BufferStride;
    uint2 boundsX = uint2(0xFFFFFFFF - _WaterLine[0 + xr], _WaterLine[1 + xr]);
    uint posX = round(dot((float2)coord.xy, rightVector) - _BoundsSS.x);
    posX = clamp(posX, boundsX.x, boundsX.y);

    // Decompress water line height
    float posY = dot((float2)coord.xy, upVector) - _BoundsSS.z;
    uint packedValue = _WaterLine[posX + 2 + xr] & 0xFFFF;
    float waterLine = packedValue - 1;

    // Normalize distance to water line
    float maxHeight = (_BoundsSS.w - _BoundsSS.z);
    float distanceToWaterLine = (floor(posY) - waterLine) / maxHeight;

    return distanceToWaterLine;
}

Texture2D<float4> _WaterCausticsDataBuffer;

float EvaluateSimulationCaustics(float3 refractedWaterPosRWS, float refractedWaterDepth, float2 distortedWaterNDC)
{
    float caustics = 0.0f;

    // TODO: Is this worth a multicompile?
    if (_CausticsIntensity != 0.0f)
    {
        // Evaluate the caustics weight
        float causticWeight = saturate(refractedWaterDepth / _CausticsPlaneBlendDistance);

        // Evaluate the normal of the surface (using partial derivatives of the absolute world pos is not possible as it is not stable enough)
        NormalData normalData;
        float4 normalBuffer = LOAD_TEXTURE2D_X_LOD(_NormalBufferTexture, distortedWaterNDC * _ScreenSize.xy, 0);
        DecodeFromNormalBuffer(normalBuffer, normalData);

        // Evaluate the triplanar weights
        float3 normalOS = mul((float3x3)_WaterSurfaceTransform_Inverse, normalData.normalWS);
        float3 triplanarW = ComputeTriplanarWeights(normalOS);

        // Convert the position to absolute world space and move the position to the water local space
        float3 causticPosAWS = GetAbsolutePositionWS(refractedWaterPosRWS) * _CausticsTilingFactor;
        float3 causticPosOS = mul(_WaterSurfaceTransform_Inverse, float4(causticPosAWS, 1.0f)).xyz;

        // Evaluate the triplanar coodinates
        float3 sampleCoord = causticPosOS / (_CausticsRegionSize * 0.5) + 0.5;
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(sampleCoord, uv0, uv1, uv2);

        // Evaluate the sharpness of the caustics based on the depth
        float sharpness = (1.0 - causticWeight) * _CausticsMaxLOD;

        // sample the caustics texture
        float3 causticsValues;
        #if defined(SHADER_STAGE_COMPUTE)
        causticsValues.x = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #else
        causticsValues.x = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
        #endif

        caustics = dot(causticsValues, triplanarW.yzx) * causticWeight * _CausticsIntensity;
    }

    // Evaluate the triplanar weights and blend the samples togheter
    return 1.0 + caustics;
}

#endif // UNDER_WATER_UTILITIES_H
