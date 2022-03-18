#ifndef PROBE_PROPAGATION
#define PROBE_PROPAGATION

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagationGlobals.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeDynamicGI.hlsl"

int _ProbeVolumeProbeCount;

RWStructuredBuffer<float3> _RadianceCacheAxis;
StructuredBuffer<float3> _PreviousRadianceCacheAxis;

int _ProbeVolumeIndex;
float _LeakMitigation;

bool IsFarFromCamera(float3 worldPosition, float rangeInFrontOfCamera, float rangeBehindCamera)
{
    float3 V = (worldPosition - _WorldSpaceCameraPos.xyz);
    float distAlongV = dot(GetViewForwardDir(), V);
    return !(distAlongV < rangeInFrontOfCamera && distAlongV > -rangeBehindCamera);
}

float3 ReadPreviousPropagationAxis(uint probeIndex, uint axisIndex)
{
    const uint index = axisIndex * _ProbeVolumeProbeCount + probeIndex;
    return _PreviousRadianceCacheAxis[index];
}

float InvalidScale(float probeValidity)
{
    float validity = pow(1.0 - probeValidity, 8.0);
    return 1.0f - lerp(_LeakMitigation, 0.0f, validity);
}

#endif // endof PROBE_PROPAGATION
