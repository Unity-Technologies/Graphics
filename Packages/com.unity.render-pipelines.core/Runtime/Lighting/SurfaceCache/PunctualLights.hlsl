#ifndef SURFACE_CACHE_PUNCTUAL_LIGHT_SAMPLE
#define SURFACE_CACHE_PUNCTUAL_LIGHT_SAMPLE

struct PunctualLight
{
    float3 position;
    float3 direction;
    float3 intensity;
    float cosAngle;
};

// This represents a sample ray shot from the light.
struct PunctualLightSample
{
    float3 hitPos;
    float3 hitNormal;
    float3 hitAlbedo;
    float3 dir;
    float3 intensity;
    float distance;
    uint hitInstanceId;
    uint hitPrimitiveIndex;
    float reciprocalDensity;

    void MarkNoHit()
    {
        hitAlbedo = -1.0f;
    }

    bool HasHit()
    {
        return all(hitAlbedo != -1.0f);
    }
};

#endif
