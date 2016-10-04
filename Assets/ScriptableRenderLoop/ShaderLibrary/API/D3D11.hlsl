// This file assume SHADER_API_D3D11 is defined

#define UNITY_UV_STARTS_AT_TOP 1
#define UNITY_REVERSED_Z 1
#define UNITY_NEAR_CLIP_VALUE (1.0)
#define VFACE FACE

#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };