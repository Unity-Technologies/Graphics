#ifndef UNITY_SCREEN_SPACE_TRACING_INCLUDED
#define UNITY_SCREEN_SPACE_TRACING_INCLUDED

// -------------------------------------------------
// Algorithm uniform parameters
// -------------------------------------------------

const float DepthPlaneBias = 1E-5;

// -------------------------------------------------
// Output
// -------------------------------------------------

struct ScreenSpaceRayHit
{
    uint2 positionSS;       // Position of the hit point (SS)
    float2 positionNDC;     // Position of the hit point (NDC)
    float linearDepth;      // Linear depth of the hit point

#ifdef DEBUG_DISPLAY
    float3 debugOutput;
#endif
};

struct ScreenSpaceHiZRaymarchInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    uint maxIterations;          // Number of iterations before failing

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

struct ScreenSpaceProxyRaycastInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    EnvLightData proxyData;

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

// -------------------------------------------------
// Utilities
// -------------------------------------------------

// Calculate the ray origin and direction in SS
// out positionSS  : (x, y, 1/depth)
// out raySS       : (x, y, 1/depth)
void CalculateRaySS(
    float3 rayOriginWS,
    float3 rayDirWS,
    uint2 bufferSize,
    out float3 positionSS,
    out float3 raySS
)
{
    float3 positionWS = rayOriginWS;
    float3 rayEndWS = rayOriginWS + rayDirWS * 10;

    float4 positionCS = ComputeClipSpacePosition(positionWS, GetWorldToHClipMatrix());
    float4 rayEndCS = ComputeClipSpacePosition(rayEndWS, GetWorldToHClipMatrix());

    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, GetWorldToHClipMatrix());
    float2 rayEndNDC = ComputeNormalizedDeviceCoordinates(rayEndWS, GetWorldToHClipMatrix());

    float3 rayStartSS = float3(
        positionNDC.xy * bufferSize,
        1.0 / positionCS.w); // Screen space depth interpolate properly in 1/z

    float3 rayEndSS = float3(
        rayEndNDC.xy * bufferSize,
        1.0 / rayEndCS.w); // Screen space depth interpolate properly in 1/z

    positionSS = rayStartSS;
    raySS = rayEndSS - rayStartSS;
}

// Check whether the depth of the ray is above the sampled depth
// Arguments are inversed linear depth
bool IsPositionAboveDepth(float rayDepth, float invLinearDepth)
{
    // as depth is inverted, we must invert the check as well
    // rayZ > HiZ <=> 1/rayZ < 1/HiZ
    return rayDepth > invLinearDepth;
}

// Sample the Depth buffer at a specific mip and linear depth
float LoadDepth(float2 positionSS, int level)
{
    float pyramidDepth = LOAD_TEXTURE2D_LOD(_DepthPyramidTexture, int2(positionSS.xy) >> level, level).r;
    float linearDepth = LinearEyeDepth(pyramidDepth, _ZBufferParams);
    return linearDepth;
}

// Sample the Depth buffer at a specific mip and return 1/linear depth
float LoadInvDepth(float2 positionSS, int level)
{
    float linearDepth = LoadDepth(positionSS, level);
    float invLinearDepth = 1 / linearDepth;
    return invLinearDepth;
}

bool CellAreEquals(int2 cellA, int2 cellB)
{
    return cellA.x == cellB.x && cellA.y == cellB.y;
}

// Calculate intersection between the ray and the depth plane
// positionSS.z is 1/depth
// raySS.z is 1/depth
float3 IntersectDepthPlane(float3 positionSS, float3 raySS, float invDepth)
{
    // The depth of the intersection with the depth plane is: positionSS.z + raySS.z * t = invDepth
    float t = (invDepth - positionSS.z) / raySS.z;

    // (t<0) When the ray is going away from the depth plane,
    //  put the intersection away.
    // Instead the intersection with the next tile will be used.
    // (t>=0) Add a small distance to go through the depth plane.
    t = t >= 0.0f ? (t + DepthPlaneBias) : 1E5;

    // Return the point on the ray
    return positionSS + raySS * t;
}

// Calculate intersection between a ray and a cell
float3 IntersectCellPlanes(
    float3 positionSS,
    float3 raySS,
    float2 invRaySS,
    int2 cellId,
    uint2 cellSize,
    int2 cellPlanes,
    float2 crossOffset
)
{
    const float SQRT_2 = sqrt(2);
    const float CellPlaneBias = 1E-2;

    // Planes to check
    int2 planes = (cellId + cellPlanes) * cellSize;
    // Hit distance to each planes
    float2 distanceToCellAxes = float2(planes - positionSS.xy) * invRaySS; // (distance to x axis, distance to y axis)
    float t = min(distanceToCellAxes.x, distanceToCellAxes.y)
        // Offset by 1E-3 to ensure cell boundary crossing
        // This assume that length(raySS.xy) == 1;
        + CellPlaneBias;
    // Interpolate screen space to get next test point
    float3 testHitPositionSS = positionSS + raySS * t;

    return testHitPositionSS;
}

#ifdef DEBUG_DISPLAY
// -------------------------------------------------
// Debug Utilities
// -------------------------------------------------

void DebugComputeCommonOutput(
    float3 rayDirWS,
    bool hitSuccessful,
    inout ScreenSpaceRayHit hit
)
{
    switch (_DebugLightingSubMode)
    {
    case DEBUGSCREENSPACETRACING_RAY_DIR_WS:
        hit.debugOutput =  rayDirWS * 0.5 + 0.5;
        break;
    case DEBUGSCREENSPACETRACING_HIT_DEPTH:
        hit.debugOutput =  frac(hit.linearDepth * 0.1);
        break;
    case DEBUGSCREENSPACETRACING_HIT_SUCCESS:
        hit.debugOutput =  hitSuccessful;
        break;
    }
}

void DebugComputeHiZOutput(
    int iteration,
    float3 startPositionSS,
    float3 rayDirSS,
    int maxIterations,
    int maxUsedLevel,
    int maxMipLevel,
    int intersectionKind,
    inout ScreenSpaceRayHit hit
)
{
    switch (_DebugLightingSubMode)
    {
    case DEBUGSCREENSPACETRACING_HI_ZPOSITION_NDC:
        hit.debugOutput =  float3(float2(startPositionSS.xy) * _ScreenSize.zw, 0);
        break;
    case DEBUGSCREENSPACETRACING_HI_ZITERATION_COUNT:
        hit.debugOutput =  float(iteration) / float(maxIterations);
        break;
    case DEBUGSCREENSPACETRACING_HI_ZRAY_DIR_NDC:
        hit.debugOutput =  float3(rayDirSS.xy * 0.5 + 0.5, frac(0.1 / rayDirSS.z));
        break;
    case DEBUGSCREENSPACETRACING_HI_ZMAX_USED_MIP_LEVEL:
        hit.debugOutput =  float(maxUsedLevel) / float(maxMipLevel);
        break;
    case DEBUGSCREENSPACETRACING_HI_ZINTERSECTION_KIND:
        hit.debugOutput =  GetIndexColor(intersectionKind);
        break;
    }
}
#endif
#endif

// -------------------------------------------------
// Algorithm: Proxy raycast
// -------------------------------------------------

#ifdef SSRTID

#define SSRT_SETTING(name, SSRTID) _SS ## SSRTID ## name
#define SSRT_FUNC(name, SSRTID) name ## SSRTID

CBUFFER_START(SSRT_FUNC(UnityScreenSpaceRaymarching, SSRTID))
int SSRT_SETTING(RayMinLevel, SSRTID);
int SSRT_SETTING(RayMaxLevel, SSRTID);
int SSRT_SETTING(RayMaxIterations, SSRTID);
float SSRT_SETTING(RayDepthSuccessBias, SSRTID);
CBUFFER_END

bool SSRT_FUNC(ScreenSpaceProxyRaycast, SSRTID)(
    ScreenSpaceProxyRaycastInput input,
    out ScreenSpaceRayHit hit
)
{
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);

    float3x3 worldToPS      = WorldToProxySpace(input.proxyData);
    float3 rayOriginPS      = WorldToProxyPosition(input.proxyData, worldToPS, input.rayOriginWS);
    float3 rayDirPS         = mul(input.rayDirWS, worldToPS);

    float projectionDistance = 0.0;

    switch(input.proxyData.influenceShapeType)
    {
        case ENVSHAPETYPE_SPHERE:
        case ENVSHAPETYPE_SKY:
        {
            projectionDistance = IntersectSphereProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
        }
        case ENVSHAPETYPE_BOX:
            projectionDistance = IntersectBoxProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
    }

    float3 hitPositionWS    = input.rayOriginWS + input.rayDirWS * projectionDistance;
    float4 hitPositionCS    = ComputeClipSpacePosition(hitPositionWS, GetWorldToHClipMatrix());
    float4 rayOriginCS      = ComputeClipSpacePosition(input.rayOriginWS, GetWorldToHClipMatrix());
    float2 hitPositionNDC   = ComputeNormalizedDeviceCoordinates(hitPositionWS, GetWorldToHClipMatrix());
    uint2 hitPositionSS     = uint2(hitPositionNDC *_ScreenSize.xy);
    float hitLinearDepth    = hitPositionCS.w;

    hit.positionNDC         = hitPositionNDC;
    hit.positionSS          = hitPositionSS;
    hit.linearDepth         = hitLinearDepth;

    bool hitSuccessful      = true;

#ifdef DEBUG_DISPLAY
    DebugComputeCommonOutput(input.rayDirWS, hitSuccessful, hit);
    
    if (input.debug)
    {
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);

        float2 rayOriginNDC         = ComputeNormalizedDeviceCoordinates(input.rayOriginWS, GetWorldToHClipMatrix());
        uint2 rayOriginSS           = uint2(rayOriginNDC * _ScreenSize.xy);

        debug.tracingModel          = SSRAYMODEL_PROXY;
        debug.loopStartPositionSSX  = rayOriginSS.x;
        debug.loopStartPositionSSY  = rayOriginSS.y;
        debug.loopStartLinearDepth  = rayOriginCS.w;
        debug.endHitSuccess         = hitSuccessful;
        debug.endLinearDepth        = hitLinearDepth;
        debug.endPositionSSX        = hitPositionSS.x;
        debug.endPositionSSY        = hitPositionSS.y;
        debug.proxyShapeType        = input.proxyData.influenceShapeType;
        debug.projectionDistance    = projectionDistance;
        
        _DebugScreenSpaceTracingData[0] = debug;
    }
#endif

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------

// Based on Yasin Uludag, 2014. "Hi-Z Screen-Space Cone-Traced Reflections", GPU Pro5: Advanced Rendering Techniques

bool SSRT_FUNC(ScreenSpaceHiZRaymarch, SSRTID)(
    ScreenSpaceHiZRaymarchInput input,
    out ScreenSpaceRayHit hit
)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = true;
    uint iteration = 0u;
    int minMipLevel = max(SSRT_SETTING(RayMinLevel, SSRTID), 0);
    int maxMipLevel = min(SSRT_SETTING(RayMaxLevel, SSRTID), int(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = min(input.maxIterations, SSRT_SETTING(RayMaxIterations, SSRTID));

    float3 startPositionSS;
    float3 raySS;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS
    );

#ifdef DEBUG_DISPLAY
    // Initialize debug variables
    int debugLoopMipMaxUsedLevel = minMipLevel;
    int debugIterationMipLevel = minMipLevel;
    uint2 debugIterationCellSize = uint2(0u, 0u);
    float3 debugIterationPositionSS = float3(0, 0, 0);
    uint debugIteration = 0u;
    uint debugIterationIntersectionKind = 0u;
    float debugIterationLinearDepthBuffer = 0;
#endif

    int intersectionKind = 0;
    float raySSLength = length(raySS.xy);
    raySS /= raySSLength;
    // Initialize raymarching
    float2 invRaySS = float2(1, 1) / raySS.xy;

    // Calculate planes to intersect for each cell
    int2 cellPlanes = sign(raySS.xy);
    float2 crossOffset = CROSS_OFFSET * cellPlanes;
    cellPlanes = clamp(cellPlanes, 0, 1);

    int currentLevel = minMipLevel;
    uint2 cellCount = bufferSize >> currentLevel;
    uint2 cellSize = uint2(1, 1) << currentLevel;

    float3 positionSS = startPositionSS;
    float invHiZDepth = 0;

    while (currentLevel >= minMipLevel)
    {
        if (iteration >= maxIterations)
        {
            hitSuccessful = false;
            break;
        }

        cellCount = bufferSize >> currentLevel;
        cellSize = uint2(1, 1) << currentLevel;


#ifdef DEBUG_DISPLAY
        // Fetch pre iteration debug values
        if (input.debug && _DebugStep >= iteration)
            debugIterationMipLevel = currentLevel;
#endif

        // Go down in HiZ levels by default
        int mipLevelDelta = -1;

        // Sampled as 1/Z so it interpolate properly in screen space.
        invHiZDepth = LoadInvDepth(positionSS.xy, currentLevel);
        intersectionKind = HIZINTERSECTIONKIND_NONE;

        if (IsPositionAboveDepth(positionSS.z, invHiZDepth))
        {
            float3 candidatePositionSS = IntersectDepthPlane(positionSS, raySS, invHiZDepth);

            intersectionKind = HIZINTERSECTIONKIND_DEPTH;

            const int2 cellId = int2(positionSS.xy) / cellSize;
            const int2 candidateCellId = int2(candidatePositionSS.xy) / cellSize;

            // If we crossed the current cell
            if (!CellAreEquals(cellId, candidateCellId))
            {
                candidatePositionSS = IntersectCellPlanes(
                    positionSS,
                    raySS,
                    invRaySS,
                    cellId,
                    cellSize,
                    cellPlanes,
                    crossOffset);

                intersectionKind = HIZINTERSECTIONKIND_CELL;

                // Go up a level to go faster
                mipLevelDelta = 1;
            }

            positionSS = candidatePositionSS;
        }

        currentLevel = min(currentLevel + mipLevelDelta, maxMipLevel);
        
#ifdef DEBUG_DISPLAY
        // Fetch post iteration debug values
        if (input.debug && _DebugStep >= iteration)
        {
            debugLoopMipMaxUsedLevel = max(debugLoopMipMaxUsedLevel, currentLevel);
            debugIterationPositionSS = positionSS;
            debugIterationLinearDepthBuffer = 1 / invHiZDepth;
            debugIteration = iteration;
            debugIterationIntersectionKind = intersectionKind;
            debugIterationCellSize = cellSize;
        }
#endif

        // Check if we are out of the buffer
        if (any(int2(positionSS.xy) > int2(bufferSize))
            || any(positionSS.xy < 0)
            )
        {
            hitSuccessful = false;
            break;
        }

        ++iteration;
    }

    hit.linearDepth = 1 / positionSS.z;
    hit.positionNDC = float2(positionSS.xy) / float2(bufferSize);
    hit.positionSS = uint2(positionSS.xy);

    if (hit.linearDepth > (1 / invHiZDepth) + SSRT_SETTING(RayDepthSuccessBias, SSRTID))
        hitSuccessful = false;

#ifdef DEBUG_DISPLAY
    DebugComputeCommonOutput(input.rayDirWS, hitSuccessful, hit);
    DebugComputeHiZOutput(
        iteration,
        startPositionSS,
        raySS,
        SSRT_SETTING(RayMaxIterations, SSRTID),
        debugLoopMipMaxUsedLevel,
        maxMipLevel,
        intersectionKind,
        hit
    );

    if (input.debug)
    {
        // Build debug structure
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);

        debug.tracingModel                  = SSRAYMODEL_HI_Z;
        debug.loopStartPositionSSX          = uint(startPositionSS.x);
        debug.loopStartPositionSSY          = uint(startPositionSS.y);
        debug.loopStartLinearDepth          = 1 / startPositionSS.z;
        debug.loopRayDirectionSS            = raySS;
        debug.loopMipLevelMax               = debugLoopMipMaxUsedLevel;
        debug.loopIterationMax              = iteration;
        debug.iterationPositionSS           = debugIterationPositionSS;
        debug.iterationMipLevel             = debugIterationMipLevel;
        debug.iteration                     = debugIteration;
        debug.iterationLinearDepthBuffer    = debugIterationLinearDepthBuffer;
        debug.iterationIntersectionKind     = debugIterationIntersectionKind;
        debug.iterationCellSizeW            = debugIterationCellSize.x;
        debug.iterationCellSizeH            = debugIterationCellSize.y;
        debug.endHitSuccess                 = hitSuccessful;
        debug.endLinearDepth                = hit.linearDepth;
        debug.endPositionSSX                = hit.positionSS.x;
        debug.endPositionSSY                = hit.positionSS.y;

        _DebugScreenSpaceTracingData[0] = debug;
    }
#endif

    return hitSuccessful;
}

#undef SSRT_SETTING
#undef SSRT_FUNC
#endif