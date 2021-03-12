#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

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
uniform float4x4 _ShadowModelMatrix;    // This is a custom model matrix without scaling
uniform float4x4 _ShadowModelInvMatrix;
uniform float3 _ShadowModelScale;       // This is the scale
uniform float  _ShadowRadius;

Varyings ProjectShadow(Attributes v)
{
    Varyings o;

    // Scale really messes things up. _ShadowModelScale is used to bake the local scale into our local position. _ShadowModelMatrix, _ShadowModelInvMatrix are model matrices without lossy scale.

    // We should change 0 to a z value to shorten shadows. This will also require additional work for per-pixel distance as we have to overproject.
    float3 vertexOS0 =  float3(v.vertex.x * _ShadowModelScale.x, v.vertex.y * _ShadowModelScale.y, 0);
    float3 vertexOS1 =  float3(v.tangent.z * _ShadowModelScale.x, v.tangent.w * _ShadowModelScale.y, 0);  // the tangent has the adjacent point stored in zw
    float3 lightPosOS = float3(mul(_ShadowModelInvMatrix, float4(_LightPos.x, _LightPos.y, _LightPos.z, 1)).xy, 0);  // Transform the light into local space
                
    float3 unnormalizedLightDir0 = vertexOS0 - lightPosOS;
    float3 unnormalizedLightDir1 = vertexOS1 - lightPosOS;

    float3 lightDir0   = normalize(unnormalizedLightDir0);
    float3 lightDir1   = normalize(unnormalizedLightDir1);
    float3 avgLightDir = normalize(lightDir0 + lightDir1);

    float  shadowLength = _ShadowRadius / dot(lightDir0, avgLightDir);
    float3 normalOS = float3(v.tangent.xy, 0); // the normal is stored in xy
    
    // Tests to make sure the light is between 0-90 degrees to the normal. Will be one if it is, zero if not.
    float3 shadowDir = lightDir0;
    float  shadowTest = ceil(dot(lightDir0, normalOS) < 0);
    float3 shadowOffset = shadowLength * shadowDir;
    
    // If we are suppose to extrude this point, then 
    float3 finalVertexOS = shadowTest * (lightPosOS + shadowOffset) + (1 - shadowTest) * vertexOS0;
    
    o.vertex = mul(GetWorldToHClipMatrix(), mul(_ShadowModelMatrix, float4(finalVertexOS, 1.0)));
    return o;
}

#endif
