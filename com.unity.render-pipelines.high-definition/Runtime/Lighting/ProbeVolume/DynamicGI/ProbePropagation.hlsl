#ifndef PROBE_PROPAGATION
#define PROBE_PROPAGATION

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagationGlobals.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeDynamicGI.hlsl"

RWStructuredBuffer<float3> _RadianceCacheAxis;
StructuredBuffer<float3> _PreviousRadianceCacheAxis;
int _RadianceCacheAxisCount;

int _ProbeVolumeIndex;
float _LeakMultiplier;

bool IsFarFromCamera(float3 worldPosition, float rangeInFrontOfCamera, float rangeBehindCamera)
{
    float3 V = (worldPosition - _WorldSpaceCameraPos.xyz);
    float distAlongV = dot(GetViewForwardDir(), V);
    if (!(distAlongV < rangeInFrontOfCamera && distAlongV > -rangeBehindCamera))
    {
        return true;
    }

    return false;
}

float3 ReadPreviousPropagationAxis(uint probeIndex, uint axisIndex)
{
    const uint index = probeIndex * NEIGHBOR_AXIS_COUNT + axisIndex;

    // TODO: remove this if check with stricter checks on construction side in C#
    if(index < (uint)_RadianceCacheAxisCount)
    {
        return _PreviousRadianceCacheAxis[index];
    }

    return 0;
}

float3 NormalizeOutputRadiance(float4 lightingAndWeight, float probeValidity)
{
    float validity = pow(1.0 - probeValidity, 8.0);
    const float invalidScale = (1.0f - lerp(_LeakMultiplier, 0.0f, validity));

    float3 radiance = lightingAndWeight.xyz * invalidScale;
    radiance *= rcp(lightingAndWeight.w);

    return radiance;
}

void WritePropagationOutput(uint probeIndex, uint axisIndex, float4 lightingAndWeight, float probeValidity)
{
    const uint index = probeIndex * NEIGHBOR_AXIS_COUNT + axisIndex;

    // TODO: remove this if check with stricter checks on construction side in C#
    if(index < (uint)_RadianceCacheAxisCount)
    {
        const float3 finalRadiance = NormalizeOutputRadiance(lightingAndWeight, probeValidity);
        _RadianceCacheAxis[index] = finalRadiance;
    }
}


#endif // endof PROBE_PROPAGATION
