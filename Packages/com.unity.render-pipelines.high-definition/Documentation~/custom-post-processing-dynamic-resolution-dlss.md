# Use custom post-processing with dynamic resolution and DLSS 

If you want to use DLSS or dynamic resolution with a custom post-processing pass, and need to interpolate or sample UVs from color, normal, or velocity, use the following functions to calculate the correct UVs:

```glsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

//...

float2 UVs = ... //the uvs coming from the interpolator
float2 correctUvs = ClampAndScaleUVForBilinearPostProcessTexture(UV); // use these uvs to sample color / normal and velocity

```