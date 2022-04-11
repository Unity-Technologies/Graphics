#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

struct Attributes
{
    float3 vertex : POSITION;
    float4 tangent: TANGENT;
    float4 extrusion : COLOR;
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

    float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
    float3 lightDir = _LightPos - vertexWS;
    lightDir.z = 0;

    // Start of code to see if this point should be extruded
    float3 lightDirection = normalize(lightDir);


    float  adjShadowRadius = 1.4143 * _ShadowRadius;  // Needed as our shadow fits like a circumscribed box around our light radius
    float3 endpoint = vertexWS + (adjShadowRadius * -lightDirection);

    float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
    float sharedShadowTest = saturate(ceil(dot(lightDirection, worldTangent)));

    // Start of code to calculate offset
    float3 vertexWS0 = TransformObjectToWorld(float3(v.extrusion.xy, 0));
    float3 vertexWS1 = TransformObjectToWorld(float3(v.extrusion.zw, 0));
    float3 shadowDir0 = vertexWS0 - _LightPos;
    shadowDir0.z = 0;
    shadowDir0 = normalize(shadowDir0);

    float3 shadowDir1 = vertexWS1 - _LightPos;
    shadowDir1.z = 0;
    shadowDir1 = normalize(shadowDir1);

    float3 shadowDir = normalize(shadowDir0 + shadowDir1);


    float3 sharedShadowOffset = sharedShadowTest * adjShadowRadius * shadowDir;

    float3 position;
    position = vertexWS + sharedShadowOffset;

    o.vertex = TransformWorldToHClip(position);

    return o;
}

#endif
