//-----------------------------------------------------------------------------
// Fixed size Kernel PCF filtering
// Ref: https://mynameismjp.wordpress.com/2015/02/18/shadow-sample-update/
// ----------------------------------------------------------------------------

// Here we define specific constant use by this shadow filtering algorithm
CBUFFER_START(UnityShadowPerLightLoop)
    float4 _ShadowMapSize; // xy size, zw inv size
CBUFFER_END

// GetPunctualShadowAttenuation is the "default" algorithm use for punctual light, material can call explicitely a particular algorithm if required (in this case users must ensure that the algorithm is present in the project).
// TODO: how this work related to shadow format ?
float GetShadowAttenuationFixedSizePCF(LightLoopContext lightLoopContext, int index, PunctualShadowData shadowData,  float3 shadowCoord, float3 shadowPosDX, float3 shadowPosDY, float2 unPositionSS)
{
    return SampleShadowCompare(lightLoopContext, index, shadowCoord).x;
}

