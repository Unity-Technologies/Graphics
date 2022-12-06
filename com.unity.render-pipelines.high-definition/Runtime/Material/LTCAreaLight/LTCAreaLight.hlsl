// Area light textures
TEXTURE2D_ARRAY(_LtcData); // We pack all Ltc data inside one texture array to limit the number of resource used

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.cs.hlsl"

#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))

// Approximate fit of BRDF with power for NdotL coefficient
void ModifyLTCTransformForDiffusePower(inout float3x3 ltcTransformDiffuse, float diffusePower, float2 uv = float2(0.0, 0.0))
{
    // reminder: value is remapped to have 0 as neutral
    diffusePower = diffusePower + 1;

    // To do this fitting, the ltcTransformDiffuse were outputed from C# by
    // - modifying the code in BRDF_Disney.cs to handle diffuse power
    // - uncommenting MenuItem in LTCTableGeneratorEditor.cs
    // - generating a few tables for various diffuse power values
    // Then each column of the table is fitted with respect to the diffuse power

#ifdef USE_DIFFUSE_LAMBERT_BRDF
    float fitted = 0.26564f;
    float w = diffusePower - 1;
    ltcTransformDiffuse._m00 += w * fitted;
    ltcTransformDiffuse._m11 += w * fitted;
#else
    // TODO: should revisit, can probably be made cheaper and more precise
    // When diffusePower > 3.5, fitting is not great but value is limited to range [1, 3] in the UI
    float w = sqrt(abs(diffusePower)) - 1;
    float w2 = sqrt(abs(diffusePower - 1));
    float y2 = uv.y * uv.y;
    float y4 = y2 * y2;

    float fitted = lerp(0.6039, 0.6588, uv.x);

    float c = lerp(0.0043359, 0.024585, uv.x);
    float d = lerp(0.0, 0.012516, uv.x);
    float fitted2 = y4*y4 * c + d * uv.y;

    float c2 = lerp(0.0039, 0.02, uv.x);
    float d2 = lerp(0.0, 0.00705, uv.x);
    float fitted3 = y4*y4 * c2 + d2 * uv.y;

    ltcTransformDiffuse._m00 += w * fitted;
    ltcTransformDiffuse._m02 -= w2 * fitted2;
    ltcTransformDiffuse._m11 += w * fitted;
    ltcTransformDiffuse._m20 += w2 * fitted3;
#endif
}
