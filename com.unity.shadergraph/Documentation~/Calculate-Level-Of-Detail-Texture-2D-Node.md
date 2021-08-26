# Calculate Level Of Detail Texture 2D Node

## Description

This node is designed to work with Texture2D. It has a [clamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-clamp) and unclamped mode. It maps to the [CalculateLevelOfDetail](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod) and [CalculateLevelOfDetailUnclamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod-unclamped) HLSL intrinsic functions.
On hardware where those intrinsics don't exist, Shader Graph determines a fallback approximation.
