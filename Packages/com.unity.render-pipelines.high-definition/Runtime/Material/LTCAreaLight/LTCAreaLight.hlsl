// Area light textures
TEXTURE2D_ARRAY(_LtcData); // We pack all Ltc data inside one texture array to limit the number of resource used

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.cs.hlsl"

#define LTC_LUT_SIZE (64)

// Approximate fit of BRDF with power for NdotL coefficient
void ModifyLambertLTCTransformForDiffusePower(inout float3x3 ltcTransformDiffuse, float diffusePower)
{
    // reminder: value is remapped to have 0 as neutral
    diffusePower = diffusePower + 1;

    // To do this fitting, the ltcTransformDiffuse were outputed from C# by
    // - modifying the code in BRDF_Disney.cs to handle diffuse power
    // - uncommenting MenuItem in LTCTableGeneratorEditor.cs
    // - generating a few tables for various diffuse power values
    // Then each column of the table is fitted with respect to the diffuse power

    float fitted = 0.26564f;
    float w = diffusePower - 1;
    ltcTransformDiffuse._m00 += w * fitted;
    ltcTransformDiffuse._m11 += w * fitted;
}

// Approximate fit of BRDF with power for NdotL coefficient
void ModifyDisneyLTCTransformForDiffusePower(inout float3x3 ltcTransformDiffuse, float diffusePower, float perceptualRoughness, float clampedNdotV)
{
    // reminder: value is remapped to have 0 as neutral
    diffusePower = diffusePower + 1;

    // To do this fitting, the ltcTransformDiffuse were outputed from C# by
    // - modifying the code in BRDF_Disney.cs to handle diffuse power
    // - uncommenting MenuItem in LTCTableGeneratorEditor.cs
    // - generating a few tables for various diffuse power values
    // Then each column of the table is fitted with respect to the diffuse power

    // TODO: should revisit, can probably be made cheaper and more precise
    // When diffusePower > 3.5, fitting is not great but value is limited to range [1, 3] in the UI
    float w = sqrt(abs(diffusePower)) - 1;
    float w2 = sqrt(abs(diffusePower - 1));
    float x  = perceptualRoughness;
    float y2 = 1 - clampedNdotV;
    float y  = sqrt(y2);
    float y4 = y2 * y2;

    float fitted = lerp(0.6039, 0.6588, x);

    float c = lerp(0.0043359, 0.024585, x);
    float d = lerp(0.0, 0.012516, x);
    float fitted2 = y4*y4 * c + d * y;

    float c2 = lerp(0.0039, 0.02, x);
    float d2 = lerp(0.0, 0.00705, x);
    float fitted3 = y4*y4 * c2 + d2 * y;

    ltcTransformDiffuse._m00 += w * fitted;
    ltcTransformDiffuse._m02 -= w2 * fitted2;
    ltcTransformDiffuse._m11 += w * fitted;
    ltcTransformDiffuse._m20 += w2 * fitted3;
}

// Fetches the transposed M^(-1) matrix need for runtime LTC evaluation.
float3x3 SampleLtcMatrix(float perceptualRoughness, float clampedNdotV, uint bsdfIndex)
{
    // sqrt(1 - cos(theta)) results in an approximately linear parametrization
    // that replaces an expensive acos() function with a simple sqrt().
    float2 uv = Remap01ToHalfTexelCoord(float2(perceptualRoughness, sqrt(1 - clampedNdotV)), LTC_LUT_SIZE);

    float3x3 invM = 0;
             invM._m22 = 1;
             invM._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, bsdfIndex, 0);

    return invM;
}