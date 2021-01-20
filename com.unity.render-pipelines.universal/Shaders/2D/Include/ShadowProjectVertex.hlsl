#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

struct Attributes\
{\
    float3 vertex : POSITION;\
    float4 tangent: TANGENT;\
    float4 extrusion : COLOR;\
};\

struct Varyings\
{\
    float4 vertex : SV_POSITION;\
};\

uniform float3 _LightPos;\
uniform float  _ShadowRadius;


Varyings ProjectShadow(Attributes v)
{
    Varyings o;
    float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
    float3 unnormalizedLightDir = _LightPos - vertexWS;
    unnormalizedLightDir.z = 0;

    // Start of code to see if this point should be extruded
    float3 lightDir = normalize(unnormalizedLightDir);
    float3 shadowDir = -lightDir;
    float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);

    // We need to solve to make sure our length will be long enough to be in our circle. Use similar triangles. h0/d0 = h1/d1 => h1 = d1 * h0 / d0 => h1 = radius * h0 / d0
    // d0 is distance to the side (from the light)
    // h0 is distance to the vertex (from the light). 
    // d1 is the light radius
    // h1 is the length of the projection (from the light)
    // shadow length = h1 - h0

    float h0 = length(float2(unnormalizedLightDir.x, unnormalizedLightDir.y));
    float d0 = dot(unnormalizedLightDir, worldTangent);
    float shadowLength = max((_ShadowRadius * h0 / d0) - d0, 0);

    // Tests to make sure the light is between 0-90 degrees to the normal. Will be one if it is, zero if not.
    float sharedShadowTest = saturate(ceil(dot(lightDir, worldTangent)));
    
    //float3 sharedShadowOffset = sharedShadowTest * shadowLength * shadowDir;
    float3 sharedShadowOffset = sharedShadowTest * shadowLength * shadowDir;

    float3 position;
    position = vertexWS + sharedShadowOffset;

    o.vertex = TransformWorldToHClip(position);

    return o;
}

#endif
