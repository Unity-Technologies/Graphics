#ifndef COMMON_UTILITIES_INCLUDED
#define COMMON_UTILITIES_INCLUDED

// remaps a value based on a in:min/max and out:min/max
// value		=		value to be remapped
// remap		=		x = min in, y = max in, z = min out, w = max out
float Remap(half value, half4 remap)
{
	return remap.z + (value - remap.x) * (remap.w - remap.z) / (remap.y - remap.x);
}

// Converts greyscale height to normal
// _tex			=		input texture(separate from a sampler)
// _sampler		=		the sampler to use
// _uv			=		uv coordinates
// _intensity	=		intensity of the effect
float3 HeightToNormal(Texture2D _tex, SamplerState _sampler, float2 _uv, half _intensity)
{
	float3 bumpSamples;
	bumpSamples.x = _tex.Sample(_sampler, _uv).x; // Sample center
	bumpSamples.y = _tex.Sample(_sampler, float2(_uv.x + _intensity / _ScreenParams.x, _uv.y)).x; // Sample U
	bumpSamples.z = _tex.Sample(_sampler, float2(_uv.x, _uv.y + _intensity / _ScreenParams.y)).x; // Sample V
	half dHdU = bumpSamples.z - bumpSamples.x;//bump U offset
	half dHdV = bumpSamples.y - bumpSamples.x;//bump V offset
	return float3(-dHdU, dHdV, 0.5);//return tangent normal
}

// Simple noise from thebookofshaders.com
// 2D Random
float2 random(float2 st){
    st = float2( dot(st,float2(127.1,311.7)), dot(st,float2(269.5,183.3)) );
    return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
}

// 2D Noise based on Morgan McGuire @morgan3d
// https://www.shadertoy.com/view/4dS3Wd
float noise (float2 st) {
    float2 i = floor(st);
    float2 f = frac(st);

    float2 u = f*f*(3.0-2.0*f);

    return lerp( lerp( dot( random(i), f),
                     dot( random(i + float2(1.0,0.0) ), f - float2(1.0,0.0) ), u.x),
                lerp( dot( random(i + float2(0.0,1.0) ), f - float2(0.0,1.0) ),
                     dot( random(i + float2(1.0,1.0) ), f - float2(1.0,1.0) ), u.x), u.y);
}

#endif // COMMON_UTILITIES_INCLUDED