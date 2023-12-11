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

#define PATHTRACING_FLAGS_BIT_COUNT     16
#define OUTPUT_FLAGS_BIT_COUNT          8

#define PATHTRACING_FLAGS_BIT_MASK      ((uint(1) << PATHTRACING_FLAGS_BIT_COUNT)-1)
#define PATHTRACING_FLAGS_CLEAR_MASK    (~PATHTRACING_FLAGS_BIT_MASK)
#define OUTPUT_FLAGS_BIT_MASK           (((uint(1) << OUTPUT_FLAGS_BIT_COUNT)-1) << PATHTRACING_FLAGS_BIT_COUNT)
#define OUTPUT_FLAGS_CLEAR_MASK         (~OUTPUT_FLAGS_BIT_MASK)

#define PATHTRACING_FLAG_SHADOW_RAY_NEEDED     (uint(1) << 0)
#define PATHTRACING_FLAG_MATERIAL_SAMPLE       (uint(1) << 1)
#define PATHTRACING_FLAG_UNLIT_MODEL           (uint(1) << 2)
#define PATHTRACING_FLAG_DUAL_SCATTERING_VIS   (uint(1) << 3)
#define PATHTRACING_FLAG_INTERPOLATE_OPACITY   (uint(1) << 4)
#define PATHTRACING_FLAG_VOLUME_INTERACTION    (uint(1) << 5)

//
// Output flags
//
// Enables various data outputs:
//   OUTPUT_FLAG_AOV                    Output Normal, Albedo and Motion Vector AOVs
//   OUTPUT_FLAG_SEPARATE_VOLUMETRICS   Output volumetrics scatering in a separate AOV
//
#define OUTPUT_FLAG_AOV                        (uint(1) << 16)
#define OUTPUT_FLAG_SEPARATE_VOLUMETRICS       (uint(1) << 17)

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
    uint    segmentID;       // Identifier for path segment and output bits (see above)

    //
    // Input/output
    //
    float3  throughput;             // Current path throughput
    float3  interactionThroughput;  // throughput incurred by the interaction traced by this ray
    float3  segmentThroughput;      // throughput incurred by the segment traced by this ray
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
    // Bit flags signifying the state of the payload informaton after tracing, as well as what data should be output
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
    float2  aovMotionVector; // Motion vector

    //
    // AOV Output
    //
    float3  aovAlbedo;       // Diffuse reflectance
    float3  aovNormal;       // Shading normal
};


bool IsOutputFlagOn(PathPayload payload, uint flagID)
{
    return (payload.flags & flagID);
}

void SetOutputFlag(inout PathPayload payload, uint flagID)
{
    payload.flags |= (flagID & OUTPUT_FLAGS_BIT_MASK);
}

void ClearOutputFlags(inout PathPayload payload)
{
    payload.flags &= OUTPUT_FLAGS_CLEAR_MASK;
}

bool IsPathTracingFlagOn(PathPayload payload, uint flagID)
{
    return (payload.flags & flagID);
}

void SetPathTracingFlag(inout PathPayload payload, uint flagID)
{
    payload.flags = payload.flags | (flagID & PATHTRACING_FLAGS_BIT_MASK);
}

void ClearPathTracingFlag(inout PathPayload payload, uint flagID)
{
    payload.flags = payload.flags & (~flagID);
}

void ClearPathTracingFlags(inout PathPayload payload)
{
    payload.flags &= PATHTRACING_FLAGS_CLEAR_MASK;
}

void SetPayloadForNextSegment(uint segmentID, inout PathPayload payload)
{
    payload.value = 0.0;
    payload.alpha = 0.0;
    payload.rayTHit = FLT_INF;
    payload.segmentID = segmentID + 1; // it's for the next one.
    payload.lightSampleValue = 0.0; // make sure we don't use the previous value
    payload.interactionThroughput = 1.0;
    payload.segmentThroughput = 1.0;
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
