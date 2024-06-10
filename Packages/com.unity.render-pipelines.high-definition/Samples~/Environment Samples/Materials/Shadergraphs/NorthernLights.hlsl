#ifndef NorthernLightsIncluded
#define NorthernLightsIncluded

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

void NorthernLights_float(in float3 viewDir, in float2 globalOffset, in float2 noiseOffset,
in int steps,
in float4 params1,
in float4 params2,
in UnityTexture2D nlTex, in UnitySamplerState ss,
in float3 baseColor, in float hueShift, in float saturationShift,
out float3 o)
{
	o = 0;

	float m = 0;
    float3 stepPos;
    float2 globalPos;
    float2 noisePos;
    float3 col = 0;
    float opacity = 0;
	
    viewDir = normalize(viewDir * float3(1, params1.w, 1));
    
    float di = 1.0 / steps;
    float h = 0;
    float he = 0;
    float energy = 1;
    
    float3 hsv = RgbToHsv(baseColor);
    float3 hsvOut = hsv;
	
    for (int i = 0; i < steps; i++)
	{	
        stepPos = viewDir - float3(0, h * params1.x, 0);
        stepPos = normalize(stepPos.xzy) * 0.5;
        noisePos = noiseOffset + stepPos * params1.y;
		
        float3 noise = nlTex.Sample(ss, noisePos.xy);
		
        globalPos = globalOffset + stepPos + (noise - 0.5) * params1.z;
        col = baseColor;
        opacity = nlTex.Sample(ss, globalPos.xy).a;
        opacity = smoothstep(params2.x, params2.y, opacity);
		
        energy = lerp(1, noise.b, params2.w);
        
        he = h;
        
        float atten = (h < params2.z) ? smoothstep(0, params2.z, h) : smoothstep(1, params2.z, h);
        atten = saturate(1 - (1 - atten) / energy);
        
        hsvOut.x = hsv.x + frac(1 + (noise.b - 0.5) * hueShift);
        hsvOut.y = hsv.y * lerp(1, 1 - saturationShift, noise.b);
        
        col = HsvToRgb(hsvOut);
        col *= opacity;
        col *= atten;
        
		o+=col;
        h += di;
    }

    o = o / steps;

	//o = pow(o/params1.z, params1.y)*params1.z;

	//o *= saturate(1-ddy(o)*params1);
}

#endif // NorthernLightsIncluded