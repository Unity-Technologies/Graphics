#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDStencilUsage.cs.hlsl"

#if defined(BILATERAL_ROUGHNESS)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.hlsl"
#endif

// Depth buffer of the current frame
TEXTURE2D_X(_DepthTexture);
TYPED_TEXTURE2D_X(uint2, _StencilTexture);
TEXTURE2D_X(_ClearCoatMaskTexture);

// ----------------------------------------------------------------------------
// Denoising Kernel
// ----------------------------------------------------------------------------

// Couple helper functions
float sqr(float value)
{
    return value * value;
}
float gaussian(float radius, float sigma)
{
    return exp(-sqr(radius / sigma));
}

// Bilateral filter parameters
#define NORMAL_WEIGHT    1.0
#define PLANE_WEIGHT     1.0
#define DEPTH_WEIGHT     1.0
#define ROUGHNESS_WEIGHT 1.0

struct BilateralData
{
    float3 position;
    float  z01;
    float  zNF;
    float3 normal;
    #if defined(BILATERAL_ROUGHNESS)
    float roughness;
    #endif
    #if defined(BILATERLAL_UNLIT)
    bool isUnlit;
    #endif
    #if defined(BILATERLAL_SSR)
    bool hasSSR;
    #endif
};

BilateralData TapBilateralData(uint2 coordSS)
{
    BilateralData key;
    PositionInputs posInput;

    if (DEPTH_WEIGHT > 0.0 || PLANE_WEIGHT > 0.0)
    {
        posInput.deviceDepth = LOAD_TEXTURE2D_X(_DepthTexture, coordSS).r;
        key.z01 = Linear01Depth(posInput.deviceDepth, _ZBufferParams);
        key.zNF = LinearEyeDepth(posInput.deviceDepth, _ZBufferParams);
    }

    #if defined(BILATERLAL_UNLIT) || defined(BILATERLAL_SSR)
    uint stencilValue = GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, coordSS));
    #endif

    // We need to define if this pixel is unlit
    #if defined(BILATERLAL_UNLIT)
    key.isUnlit = (stencilValue & STENCILUSAGE_IS_UNLIT) != 0;
    #endif

    if (PLANE_WEIGHT > 0.0)
    {
        posInput = GetPositionInput(coordSS, _ScreenSize.zw, posInput.deviceDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        key.position = posInput.positionWS;
    }

    if ((NORMAL_WEIGHT > 0.0) || (PLANE_WEIGHT > 0.0))
    {
        NormalData normalData;
        const float4 normalBuffer = LOAD_TEXTURE2D_X(_NormalBufferTexture, coordSS);
        DecodeFromNormalBuffer(normalBuffer, normalData);
        key.normal = normalData.normalWS;

    #ifdef BILATERAL_ROUGHNESS
        float4 coatMask = LOAD_TEXTURE2D_X(_ClearCoatMaskTexture, coordSS);
        key.roughness = HasClearCoatMask(coatMask) ? CLEAR_COAT_SSR_PERCEPTUAL_ROUGHNESS : normalData.perceptualRoughness;
    #endif

    #if defined(BILATERLAL_SSR)
    key.hasSSR = (stencilValue & STENCILUSAGE_TRACE_REFLECTION_RAY) != 0;
        #ifdef BILATERAL_ROUGHNESS
        key.hasSSR = key.hasSSR && (_RaytracingReflectionMinSmoothness <= PerceptualRoughnessToPerceptualSmoothness(key.roughness));
        #endif
    #endif
    }

    return key;
}

BilateralData TapBilateralData(uint2 coordSS, float depthValue)
{
    BilateralData key;
    PositionInputs posInput;

    if (DEPTH_WEIGHT > 0.0 || PLANE_WEIGHT > 0.0)
    {
        posInput.deviceDepth = depthValue;
        key.z01 = Linear01Depth(posInput.deviceDepth, _ZBufferParams);
        key.zNF = LinearEyeDepth(posInput.deviceDepth, _ZBufferParams);
    }

    #if defined(BILATERLAL_UNLIT) || defined(BILATERLAL_SSR)
    uint stencilValue = GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, coordSS));
    #endif

    // We need to define if this pixel is unlit
    #if defined(BILATERLAL_UNLIT)
    key.isUnlit = (stencilValue & STENCILUSAGE_IS_UNLIT) != 0;
    #endif

    if (PLANE_WEIGHT > 0.0)
    {
        posInput = GetPositionInput(coordSS, _ScreenSize.zw, posInput.deviceDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        key.position = posInput.positionWS;
    }

    if ((NORMAL_WEIGHT > 0.0) || (PLANE_WEIGHT > 0.0))
    {
        NormalData normalData;
        const float4 normalBuffer = LOAD_TEXTURE2D_X(_NormalBufferTexture, coordSS);
        DecodeFromNormalBuffer(normalBuffer, normalData);
        key.normal = normalData.normalWS;
    #ifdef BILATERAL_ROUGHNESS
        key.roughness = normalData.perceptualRoughness;
    #endif
    }

    #if defined(BILATERLAL_SSR)
    key.hasSSR = (stencilValue & STENCILUSAGE_TRACE_REFLECTION_RAY) != 0;
        #ifdef BILATERAL_ROUGHNESS
        key.hasSSR = key.hasSSR && (_RaytracingReflectionMinSmoothness <= PerceptualRoughnessToPerceptualSmoothness(key.roughness));
        #endif
    #endif

    return key;
}

float ComputeBilateralWeight(BilateralData center, BilateralData tap)
{
    float depthWeight    = 1.0;
    float normalWeight   = 1.0;
    float planeWeight    = 1.0;
    float roughnessWeight    = 1.0;

    if (DEPTH_WEIGHT > 0.0)
    {
        depthWeight = max(0.0, 1.0 - abs(tap.z01 - center.z01) * DEPTH_WEIGHT);
    }

    if (NORMAL_WEIGHT > 0.0)
    {
        const float normalCloseness = sqr(sqr(max(0.0, dot(tap.normal, center.normal))));
        const float normalError = 1.0 - normalCloseness;
        normalWeight = max(0.0, (1.0 - normalError * NORMAL_WEIGHT));
    }

    if (PLANE_WEIGHT > 0.0)
    {
        // Change in position in camera space
        const float3 dq = center.position - tap.position;

        // How far away is this point from the original sample
        // in camera space? (Max value is unbounded)
        const float distance2 = dot(dq, dq);

        // How far off the expected plane (on the perpendicular) is this point? Max value is unbounded.
        const float planeError = max(abs(dot(dq, tap.normal)), abs(dot(dq, center.normal)));

        planeWeight = (distance2 < 0.0001) ? 1.0 :
            pow(max(0.0, 1.0 - 2.0 * PLANE_WEIGHT * planeError / sqrt(distance2)), 2.0);
    }

    #ifdef BILATERAL_ROUGHNESS
    if (ROUGHNESS_WEIGHT > 0.0)
    {
        roughnessWeight = max(0.0, 1.0 - abs(tap.roughness - center.roughness) * ROUGHNESS_WEIGHT);
    }
    #endif
    return depthWeight * normalWeight * planeWeight * roughnessWeight;
}
