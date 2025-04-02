#pragma once

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"

// Overrides the transform functions that would use object matrices in the fog as they are not available due to the indirect draw
// Instead we can re-build the object matrix from the OBB of the fog object

float4x4 BuildWorldToObjectMatrixFromLocalFogOBB()
{
    float3x3 rotation = float3x3(
        _VolumetricMaterialObbRight.xyz,
        _VolumetricMaterialObbUp.xyz,
        cross(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz)
    );

    // inverse rotation
    rotation = transpose(rotation);

    // inverse translation
    float3 inverseTranslation = -(mul(_VolumetricMaterialObbCenter.xyz, rotation));

    // Build matrix
    float4x4 objectMatrix = 0;
    objectMatrix._m00_m10_m20 = rotation[0];
    objectMatrix._m01_m11_m21 = rotation[1];
    objectMatrix._m02_m12_m22 = rotation[2];
    objectMatrix._m03_m13_m23_m33 = float4(inverseTranslation, 1);

    return objectMatrix;
}

float3 TransformWorldToObjectFog(float3 positionRWS)
{
    float3 posWS = GetAbsolutePositionWS(positionRWS);
    return mul(BuildWorldToObjectMatrixFromLocalFogOBB(), float4(posWS, 1));
}
