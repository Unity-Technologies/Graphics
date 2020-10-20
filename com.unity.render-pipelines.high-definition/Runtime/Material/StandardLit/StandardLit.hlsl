#ifndef STANDARD_LIT_HLSL
#define STANDARD_LIT_HLSL
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"

void EncodeIntoStandardGBuffer( StandardBSDFData standardBSDFData
                        , out GBufferType0 outGBuffer0
                        , out GBufferType1 outGBuffer1
                        , out GBufferType2 outGBuffer2
                        , out GBufferType3 outGBuffer3
                        )
{
	// GBuffer0
    outGBuffer0 = float4(standardBSDFData.baseColor, standardBSDFData.specularOcclusion);

	// GBuffer1
    NormalData normalData;
    normalData.normalWS = standardBSDFData.normalWS;
    normalData.perceptualRoughness = standardBSDFData.perceptualRoughness;
    EncodeIntoNormalBuffer(normalData, outGBuffer1);

	// GBuffer2
    outGBuffer2.rgb = FastLinearToSRGB(standardBSDFData.fresnel0);
    outGBuffer2.a  = PackFloatInt8bit(standardBSDFData.coatMask, GBUFFER_LIT_STANDARD, 8);

    // GBuffer3
    outGBuffer3 = float4(standardBSDFData.emissiveAndBaked, 0.0);
    outGBuffer3 *= GetCurrentExposureMultiplier();
}
#endif // STANDARD_LIT_HLSL
