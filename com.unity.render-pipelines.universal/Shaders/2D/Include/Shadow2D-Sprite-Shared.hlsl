#if !defined(SPRITE_SHADOW_SHARED)
#define SPRITE_SHADOW_SHARED

struct Attributes
{
    float3 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float4      _MainTex_ST;
half4	    _LightColor;
float       _ShadowIntensity;

Varyings vert(Attributes v)
{
    Varyings o;
    float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
    o.vertex = TransformWorldToHClip(vertexWS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

#endif
