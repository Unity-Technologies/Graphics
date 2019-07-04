#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void RayTracingEncodeIntoGBuffer( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
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
						, out bool forwardOnly
                        )
{
    // Given that we will be multiplying the final color by the current exposure multiplier outside of this function, we need to make sure that
    // the unlit color is not impacted by that. Thus, we multiply it by the inverse of the current exposure multiplier.
    outGBuffer3 = float4(surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor, 1.0);
    forwardOnly = true;
}
#endif