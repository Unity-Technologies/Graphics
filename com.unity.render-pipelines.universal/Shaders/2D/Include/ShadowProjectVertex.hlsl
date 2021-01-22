#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

#define SQUARE_ROOT_2 1.4142136f

struct Attributes
{
    float3 vertex : POSITION;
    float4 tangent: TANGENT;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
};

uniform float3 _LightPos;
uniform float  _ShadowRadius;


Varyings ProjectShadow(Attributes v)
{
    Varyings o;
    float3 vertexWS0 = TransformObjectToWorld(float3(v.vertex.xy,0));  // This should be in world space
    float3 vertexWS1 = TransformObjectToWorld(float3(v.tangent.zw,0));  // the tangent has 
    float3 unnormalizedLightDir0 = _LightPos - vertexWS0;
    float3 unnormalizedLightDir1 = _LightPos - vertexWS1;

    float3 lightDir0   = normalize(unnormalizedLightDir0);
    float3 lightDir1   = normalize(unnormalizedLightDir1);
    float3 avgLightDir = normalize(lightDir0 + lightDir1);

    // Start of code to see if this point should be extruded


    float shadowLength = _ShadowRadius / dot(-lightDir0, -avgLightDir);
    
    float3 normalWS = TransformObjectToWorldDir(float3(v.tangent.xy, 0));
    

    // Tests to make sure the light is between 0-90 degrees to the normal. Will be one if it is, zero if not.
    float3 shadowDir = -lightDir0;
    float  shadowTest = dot(lightDir0, normalWS) < 0;
    float3 shadowOffset = shadowTest * shadowLength * shadowDir;

    float3 position;
    position = shadowTest * (_LightPos + shadowOffset) + (1-shadowTest) * vertexWS0;

    o.vertex = TransformWorldToHClip(position);

    return o;
}

#endif
