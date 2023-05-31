#ifndef REBLUR_BLUR_UTILITIES_H_
#define REBLUR_BLUR_UTILITIES_H_

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_Utilities.hlsl"

// Poisson disk distribution
#define POISSON_SAMPLE_COUNT 8
static const float3 k_PoissonDiskSamples[POISSON_SAMPLE_COUNT] =
{
    // https://www.desmos.com/calculator/abaqyvswem
    float3( -1.00             ,  0.00             , 1.0 ),
    float3(  0.00             ,  1.00             , 1.0 ),
    float3(  1.00             ,  0.00             , 1.0 ),
    float3(  0.00             , -1.00             , 1.0 ),
    float3( -0.25 * sqrt(2.0) ,  0.25 * sqrt(2.0) , 0.5 ),
    float3(  0.25 * sqrt(2.0) ,  0.25 * sqrt(2.0) , 0.5 ),
    float3(  0.25 * sqrt(2.0) , -0.25 * sqrt(2.0) , 0.5 ),
    float3( -0.25 * sqrt(2.0) , -0.25 * sqrt(2.0) , 0.5 )
};

float GetGaussianWeight( float r )
{
    return exp( -0.66 * r * r ); // assuming r is normalized to 1
}

float ComputeBlurRadius(float roughness, float maxRadius)
{
	return maxRadius * GetSpecMagicCurve2(roughness);
}

#endif // REBLUR_BLUR_UTILITIES_H_
