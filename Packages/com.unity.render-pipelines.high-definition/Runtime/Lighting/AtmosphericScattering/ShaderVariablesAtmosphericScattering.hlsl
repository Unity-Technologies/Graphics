#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"

GLOBAL_TEXTURECUBE_ARRAY(_SkyTexture, RAY_TRACING_SKY_TEXTURE_REGISTER);
GLOBAL_RESOURCE(StructuredBuffer<float4>, _AmbientProbeData, RAY_TRACING_AMBIENT_PROBE_DATA_REGISTER);

#define AMBIENT_PROBE_BUFFER 1

#define _MipFogNear         _MipFogParameters.x
#define _MipFogFar          _MipFogParameters.y
#define _MipFogMaxMip       _MipFogParameters.z

#define _FogColor           _FogColor
