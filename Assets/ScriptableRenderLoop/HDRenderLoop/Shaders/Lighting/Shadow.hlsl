//-----------------------------------------------------------------------------
// Shadow
// Ref: https://mynameismjp.wordpress.com/2015/02/18/shadow-sample-update/
// ----------------------------------------------------------------------------

//-------------------------------------------------------------------------------------------------
// Calculates the offset to use for sampling the shadow map, based on the surface normal
//-------------------------------------------------------------------------------------------------
float3 GetShadowPosOffset(float NdotL, float3 normalWS, float2 invShadowMapSize)
{
    float texelSize = 2.0 * invShadowMapSize.x;
    float offsetScaleNormalize = saturate(1.0 - NdotL);
   // return texelSize * OffsetScale * offsetScaleNormalize * normalWS;
    return texelSize * offsetScaleNormalize * normalWS;
}

// TODO: implement various algorithm

// GetShadowAttenuation is the "default" algorithm use, material can code explicitely a particular algorithm if required.
// TODO: how this work related to shadow format ?
float GetShadowAttenuation(LightLoopContext lightLoopContext, int index, float3 shadowCoord, float3 shadowPosDX, float3 shadowPosDY, float2 unPositionSS)
{
    // TODO: How to support a Gather sampling with such abstraction...
    return SampleShadowCompare(lightLoopContext, index, shadowCoord);
}