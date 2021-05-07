//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef PROBEPLACEMENT_CS_HLSL
#define PROBEPLACEMENT_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.ProbePlacement+GPUProbeVolumeOBB
// PackingRules = Exact
struct GPUProbeVolumeOBB
{
    float3 corner;
    float3 X;
    float3 Y;
    float3 Z;
    int minSubdivisionLevel;
    int maxSubdivisionLevel;
    float geometryDistanceOffset;
};

//
// Accessors for UnityEngine.Experimental.Rendering.ProbePlacement+GPUProbeVolumeOBB
//
float3 GetCorner(GPUProbeVolumeOBB value)
{
    return value.corner;
}
float3 GetX(GPUProbeVolumeOBB value)
{
    return value.X;
}
float3 GetY(GPUProbeVolumeOBB value)
{
    return value.Y;
}
float3 GetZ(GPUProbeVolumeOBB value)
{
    return value.Z;
}
int GetMinSubdivisionLevel(GPUProbeVolumeOBB value)
{
    return value.minSubdivisionLevel;
}
int GetMaxSubdivisionLevel(GPUProbeVolumeOBB value)
{
    return value.maxSubdivisionLevel;
}
float GetGeometryDistanceOffset(GPUProbeVolumeOBB value)
{
    return value.geometryDistanceOffset;
}

#endif
