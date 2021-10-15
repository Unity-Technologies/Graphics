#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VolumeVoxelisationCommon.hlsl"

#define CustomDensityVolumeBase_Included

float4x4 VolumeMatrix;
float4x4 InvVolumeMatrix;

float4 VolumeTime;
float3 FogAlbedo;
float FogMeanFreePath;

StructuredBuffer<OrientedBBox> _VolumeBounds;
