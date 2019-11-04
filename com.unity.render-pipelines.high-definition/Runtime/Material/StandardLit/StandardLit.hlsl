#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.cs.hlsl"

void EncodeIntoStandardGBuffer( StandardBSDFData standardBSDFData
                        , out GBufferType0 outGBuffer0
                        , out GBufferType1 outGBuffer1
                        , out GBufferType2 outGBuffer2
                        , out GBufferType3 outGBuffer3
#if GBUFFERMATERIAL_COUNT > 4
                        , out GBufferType4 outGBuffer4
#endif
#if GBUFFERMATERIAL_COUNT > 5
                        , out GBufferType5 outGBuffer5
#endif
                        )
{
	// GBuffer0
    outGBuffer0 = float4(standardBSDFData.baseColor, standardBSDFData.specularOcclusion);

	// GBuffer1
    NormalData normalData;
    normalData.normalWS = standardBSDFData.normalWS;
    normalData.perceptualRoughness = standardBSDFData.perceptualRoughness;
    EncodeIntoNormalBuffer(normalData, uint2(0, 0), outGBuffer1);

	// GBuffer2
    outGBuffer2.rgb = FastLinearToSRGB(standardBSDFData.fresnel0);
    outGBuffer2.a  = PackFloatInt8bit(standardBSDFData.coatMask, GBUFFER_LIT_STANDARD, 8);

    // GBuffer3
    outGBuffer3 = float4(standardBSDFData.emissiveAndBaked, 0.0);
    outGBuffer3 *= GetCurrentExposureMultiplier();

#ifdef LIGHT_LAYERS
    OUT_GBUFFER_LIGHT_LAYERS = float4(0.0, 0.0, 0.0, standardBSDFData.renderingLayers / 255.0);
#endif

#ifdef SHADOWS_SHADOWMASK
    OUT_GBUFFER_SHADOWMASK = standardBSDFData.shadowMask;
#endif
}
