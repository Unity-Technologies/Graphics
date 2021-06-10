# Gather Texture 2D Node

## Description

This node is designed to work with Texture2D, and takes four samples (red component only) to use for bilinear interpolation during texture sampling. It maps to the [Gather](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-gather) HLSL intrinsic.  On hardware where this intrinsic does not exist, Shader Graph determines a fallback approximation.
On platforms using the Metal graphics API (iOS, macOS), the Gather intrinsic takes an integer offset argument (int2) that the target platform can directly clamp. This affects texture sample, sample_compare, gather, and gather_compare functions for 2D textures, because they take an offset argument. Their offset value must be within the range -8 -> 7 to avoid it being clamped by the Metal API.
