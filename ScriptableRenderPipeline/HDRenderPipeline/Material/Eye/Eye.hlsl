#ifndef EYE
#define EYE

void EyeParallax(inout float2 uv, float3 normalWS, float3 viewWS)
{
    float height = _EyeIrisDepth * saturate( 1.0 - 0.736 * _EyeIrisRadius * _EyeIrisRadius );

	// Refraction
	float w = _EyeIOR * dot( normalWS, viewWS );
    float k = sqrt( 1.0 + ( w - _EyeIOR ) * ( w + _EyeIOR ) );
    float3 refractedW = ( w - k ) * normalWS - _EyeIOR * viewWS;

    float cosAlpha = dot(_EyeLookVector, -refractedW);
    float dist = height / cosAlpha;

	float3 offsetW = dist * refractedW;
    float2 offsetL = mul(offsetW, (float3x2)GetObjectToWorldMatrix());

    float m = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv).r;
	uv += float2(m, -m) * offsetL;
}

#endif//EYE