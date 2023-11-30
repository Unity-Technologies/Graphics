#ifndef UNITY_PATH_TRACING_PAYLOAD_INCLUDED
#define UNITY_PATH_TRACING_PAYLOAD_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

//
// Segment ID
//
// Identifies segments (or rays) along our path:
//   0:                          Camera ray
//   1 - SEGMENT_ID_MAX_DEPTH:   Continuation ray (ID == depth)
//   SEGMENT_ID_TRANSMISSION:    Transmission (or Shadow) ray
//   SEGMENT_ID_RANDOM_WALK:     Random walk ray (used in SSS)
//   SEGMENT_ID_DUAL_SCATTERING: Dual scattering ray (used in hair multiple scattering optimization)
//
#define SEGMENT_ID_TRANSMISSION        (UINT_MAX - 0)
#define SEGMENT_ID_RANDOM_WALK         (UINT_MAX - 1)
#define SEGMENT_ID_DUAL_SCATTERING     (UINT_MAX - 2)
#define SEGMENT_ID_DUAL_SCATTERING_VIS (UINT_MAX - 3)
#define SEGMENT_ID_NEAREST_HIT         (UINT_MAX - 4)
#define SEGMENT_ID_MAX_DEPTH           (UINT_MAX - 5)


#define PATHTRACING_FLAG_SHADOW_RAY_NEEDED     (1 << 0)
#define PATHTRACING_FLAG_MATERIAL_SAMPLE       (1 << 1)
#define PATHTRACING_FLAG_UNLIT_MODEL           (1 << 2)
#define PATHTRACING_FLAG_DUAL_SCATTERING_VIS   (1 << 3)
#define PATHTRACING_FLAG_INTERPOLATE_OPACITY   (1 << 4)
#define PATHTRACING_FLAG_VOLUME_INTERACTION    (1 << 5)

// Path Tracing Payload
struct PathPayload
{

    //
    // Basic ray tracing information
    //      Tracing information, throughput, distance, etc.
    // 

    //
    // Input
    //
    uint2   pixelCoord;      // Pixel coordinates from which the path emanates
    uint    segmentID;       // Identifier for path segment (see above)

    //
    // Input/output
    //
    float3  throughput;      // Current path throughput
    float   maxRoughness;    // Current maximum roughness encountered along the path
    RayCone cone;            // Ray differential information (not used currently)

    //
    // Output
    //
    float3  value;           // Main value (radiance, or normal for random walk)
    float   alpha;           // Opacity value (computed from transmittance)
    float   rayTHit;         // Ray parameter, used either for current or next hit

    //
    // Sampling information
    //      The closest hit shader will sample NEE and Material Sample directions and store the necessary information in the payload
    // 

    //
    // Bit flags signifying the state of the payload informaton after tracing
    //
    uint flags;

    //
    // Information on how the LightList was created; needed for correct MIS weighting on LightEval
    //
    float4 lightListParams;

    //
    // Material Sample information
    // 
    float3 materialSampleRayOrigin;
    float3 materialSampleRayDirection;
    // This is input/output as it is used for MIS weighting on the following interaction
    float materialSamplePdf;
    
    // 
    // Direct lighting (dl) query information 
    //
    float3 lightSampleRayOrigin;
    float3 lightSampleRayDirection;
    float  lightSampleRayDistance;
    float2 lightSampleShadowOpacityAndShadowTint;
    float3 lightSampleValue;
    float3 lightSampleShadowColor;


    //
    // AOV information
    // 

    //
    // AOV Input/output
    //
    float2  aovMotionVector; // Motion vector (also serve as on/off AOV switch)

    //
    // AOV Output
    //
    float3  aovAlbedo;       // Diffuse reflectance
    float3  aovNormal;       // Shading normal
};


bool IsPathTracingFlagOn(PathPayload payload, uint flagID)
{
    return (payload.flags & flagID);
}

void SetPathTracingFlag(inout PathPayload payload, uint flagID)
{
    payload.flags = payload.flags | flagID;
}

void ClearPathTracingFlag(inout PathPayload payload, uint flagID)
{
    payload.flags = payload.flags & (~flagID);
}

void ClearPathTracingFlags(inout PathPayload payload)
{
    payload.flags = 0;
}

void SetPayloadForNextSegment(uint segmentID, inout PathPayload payload)
{
    payload.value = 0.0;
    payload.alpha = 0.0;
    payload.rayTHit = FLT_INF;
    payload.segmentID = segmentID + 1; // it's for the next one.
    payload.lightSampleValue = 0.0; // make sure we don't use the previous value
    ClearPathTracingFlags(payload);
    // Don't touch throughput, materialSamplePdf or any other value that is Input and Output. 
}

void PushLightSampleQuery(float3 origin, float3 direction, float tMax, float3 lighting, float shadowOpacity, inout PathPayload payload)
{
    SetPathTracingFlag(payload, PATHTRACING_FLAG_SHADOW_RAY_NEEDED);
    payload.lightSampleRayOrigin = origin;
    payload.lightSampleRayDirection = direction;
    payload.lightSampleRayDistance = tMax;
    payload.lightSampleShadowOpacityAndShadowTint.x = shadowOpacity;
    payload.lightSampleValue = lighting;
}

void PushLightSampleQueryUnlit(float3 origin, float3 direction, float tMax, float3 lighting, float shadowOpacity, float3 shadowColor, inout PathPayload payload)
{
    PushLightSampleQuery(origin, direction, tMax, lighting, shadowOpacity, payload);
    payload.lightSampleShadowColor = shadowColor;
}

void PushMaterialSampleQuery(float3 origin, float3 direction, inout PathPayload payload)
{
    SetPathTracingFlag(payload, PATHTRACING_FLAG_MATERIAL_SAMPLE);
    payload.materialSampleRayOrigin = origin;
    payload.materialSampleRayDirection = direction;
}

#endif // UNITY_PATH_TRACING_PAYLOAD_INCLUDED
