#if !defined(NORMALS_RENDERING_PASS)
#define NORMALS_RENDERING_PASS

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
uniform float4 _NormalMap_ST;  // Is this the right way to do this?

Varyings NormalsRenderingVertex(Attributes attributes)
{
	Varyings o;
    o.positionCS = TransformObjectToHClip(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _NormalMap); 
	o.uv = attributes.uv;
	o.color = attributes.color;
    return o;
}

float4 NormalsRenderingFragment(Varyings i) : SV_Target
{
	float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

	float4 normalColor;
	normalColor.rgb = 0.5 * (UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv)) + 1);
	normalColor.a = mainTex.a;

	return normalColor;
}

#endif
