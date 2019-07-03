#ifdef HAS_LIGHTLOOP
IndirectLighting EvaluateBSDF_RaytracedRefraction(LightLoopContext lightLoopContext,
                                                    PreLightData preLightData,
                                                    float3 transmittedColor)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    return lighting;
}
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void RayTracingEncodeIntoGBuffer(SurfaceData surfaceData
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
    outGBuffer3 = float4(1.0, 0.0, 1.0, 1.0);
    forwardOnly = false;
}
#endif