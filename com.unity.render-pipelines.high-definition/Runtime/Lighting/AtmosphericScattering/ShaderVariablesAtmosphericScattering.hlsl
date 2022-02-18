TEXTURECUBE_ARRAY(_SkyTexture);
StructuredBuffer<float4>    _AmbientProbeData;

#define _MipFogNear         _MipFogParameters.x
#define _MipFogFar          _MipFogParameters.y
#define _MipFogMaxMip       _MipFogParameters.z

#define _FogColor           _FogColor
