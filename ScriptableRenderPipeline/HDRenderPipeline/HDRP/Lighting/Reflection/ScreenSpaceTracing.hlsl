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

void FillScreenSpaceRaymarchingHitDebug(
    uint2 bufferSize,
    float3 rayDirWS,
    float3 rayDirSS,
    float3 startPositionSS,
    bool hitSuccessful,
    int iteration,
    int maxIterations,
    int maxUsedLevel,
    int maxMipLevel,
    int intersectionKind,
    inout ScreenSpaceRayHit hit
)
{
    float3 debugOutput = float3(0, 0, 0);
    switch (_DebugLightingSubMode)
    {
    case DEBUGSCREENSPACETRACING_POSITION_NDC:
        debugOutput =  float3(float2(startPositionSS.xy) / bufferSize, 0);
        break;
    case DEBUGSCREENSPACETRACING_DIR_WS:
        debugOutput =  rayDirWS * 0.5 + 0.5;
        break;
    case DEBUGSCREENSPACETRACING_DIR_NDC:
        debugOutput =  float3(rayDirSS.xy * 0.5 + 0.5, frac(0.1 / rayDirSS.z));
        break;
    case DEBUGSCREENSPACETRACING_HIT_DEPTH:
        debugOutput =  frac(hit.linearDepth * 0.1);
        break;
    case DEBUGSCREENSPACETRACING_HIT_SUCCESS:
        debugOutput =  hitSuccessful;
        break;
    case DEBUGSCREENSPACETRACING_ITERATION_COUNT:
        debugOutput =  float(iteration) / float(maxIterations);
        break;
    case DEBUGSCREENSPACETRACING_MAX_USED_LEVEL:
        debugOutput =  float(maxUsedLevel) / float(maxMipLevel);
        break;
    case DEBUGSCREENSPACETRACING_INTERSECTION_KIND:
        debugOutput =  GetIndexColor(intersectionKind);
        break;
    }
    hit.debugOutput = debugOutput;
}

void FillScreenSpaceRaymarchingPreLoopDebug(
    float3 startPositionSS,
    inout ScreenSpaceTracingDebug debug
)
{
    debug.startPositionSSX = uint(startPositionSS.x);
    debug.startPositionSSY = uint(startPositionSS.y);
    debug.startLinearDepth = 1 / startPositionSS.z;
}

void FillScreenSpaceRaymarchingPostLoopDebug(
    int maxUsedLevel,
    int iteration,
    float3 raySS,
    bool hitSuccess,
    ScreenSpaceRayHit hit,
    inout ScreenSpaceTracingDebug debug
)
{
    debug.levelMax = maxUsedLevel;
    debug.iterationMax = iteration;
    debug.raySS = raySS;
    debug.resultHitDepth = hit.linearDepth;
    debug.endPositionSSX = hit.positionSS.x;
    debug.endPositionSSY = hit.positionSS.y;
    debug.hitSuccess = hitSuccess;
}

void FillScreenSpaceRaymarchingPreIterationDebug(
    int iteration,
    int currentLevel,
    inout ScreenSpaceTracingDebug debug
)
{
    if (_DebugStep >= iteration)
        debug.level = currentLevel;
}

void FillScreenSpaceRaymarchingPostIterationDebug(
    int iteration,
    uint2 cellSize,
    float3 positionSS,
    float invHiZDepth,
    uint intersectionKind,
    inout ScreenSpaceTracingDebug debug
)
{
    if (_DebugStep >= iteration)
    {
        debug.cellSizeW = cellSize.x;
        debug.cellSizeH = cellSize.y;
        debug.positionSS = positionSS;
        debug.hitLinearDepth = 1 / positionSS.z;
        debug.iteration = iteration;
        debug.hiZLinearDepth = 1 / invHiZDepth;
        debug.intersectionKind = intersectionKind;
    }
}
#endif
#endif

// -------------------------------------------------
// Algorithm: Proxy raycast
// -------------------------------------------------

#ifdef SSRTID

#define SSRT_SETTING(name) _SS#SSRTID#name
#define SSRT_FUNC(name) name#SSRTID

CBUFFER_START(SSRT_FUNC(UnityScreenSpaceRaymarching)
int SSRT_SETTING(RayMinLevel);
int SSRT_SETTING(RayMaxLevel);
int SSRT_SETTING(RayMaxIterations);
float SSRT_SETTING(RayDepthSuccessBias);
CBUFFER_END


bool SSRT_FUNC(ScreenSpaceProxyRaycast)(
    ScreenSpaceEstimateRaycastInput input,
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
            projectionDistance = IntersectSphereProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
        case ENVSHAPETYPE_BOX:
            projectionDistance = IntersectBoxProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
    }

    float3 hitPositionWS    = input.rayOriginWS + input.rayDirWS * projectionDistance;
    float3 hitPositionCS    = ComputeClipSpacePosition(hitPositionWS, GetWorldToHClipMatrix());
    float2 hitPositionNDC   = ComputeNormalizedDeviceCoordinates(hitPositionWS, GetWorldToHClipMatrix());
    uint2 hitPositionSS     = uint2(hitPositionNDC *_ScreenSize.xy);
    float hitLinearDepth    = hitPositionCS.w;

    float linearDepth       = LoadDepth(hitPositionSS, 0);

    input.positionNDC       = hitPositionNDC;
    input.positionSS        = hitPositionSS;
    input.linearDepth       = hitLinearDepth;

    bool hitSuccessful      = linearDepth >= hitLinearDepth;

#ifdef DEBUG_DISPLAY
    FillScreenSpaceRaymarchingHitDebug(
        uint2(_ScreenSize.xy),          // bufferSize
        input.rayDirWS,
        float3(0.0, 0.0, 0.0),          // rayDirSS
        float3(0.0, 0.0, 0.0),          // startPositionSS
        hitSuccessful,
        0,                              // iteration,
        1,                              // maxIterations,
        0,                              // maxUsedLevel,
        0,                              // maxMipLevel,
        3,                              // intersectionKind,
        inout hit
    );
    
    if (input.debug)
    {
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);
        FillScreenSpaceRaymarchingPreLoopDebug(
            float3(0.0, 0.0, 0.0),      // startPositionSS
            inout debug
        );
        FillScreenSpaceRaymarchingPostLoopDebug(
            0,                          // maxUsedLevel,
            0,                          // iteration,
            float3(0.0, 0.0, 0.0),      // raySS,
            hitSuccessful,
            hit,
            inout debug
        );
        FillScreenSpaceRaymarchingPreIterationDebug(
            0,                          // iteration,
            0,                          // currentLevel,
            inout debug
        );
        FillScreenSpaceRaymarchingPostIterationDebug(
            0,                          // iteration,
            uint2(0, 0),                // cellSize,
            float3(0.0, 0.0, 0.0),      // positionSS,
            1.0 / input.linearDepth,    // invHiZDepth,
            3,                          // intersectionKind,
            inout debug
        );
        _DebugScreenSpaceTracingData[0] = debug;
    }
#endif

    return false;
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------

// Based on Yasin Uludag, 2014. "Hi-Z Screen-Space Cone-Traced Reflections", GPU Pro5: Advanced Rendering Techniques

bool SSRT_FUNC(ScreenSpaceHiZRaymarch)(
    ScreenSpaceHiZRaymarchInput input,
    out ScreenSpaceRayHit hit
)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = true;
    uint iteration = 0u;
    int minMipLevel = max(SSRT_SETTING(MinLevel), 0);
    int maxMipLevel = min(SSRT_SETTING(MaxLevel), int(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = min(input.maxIterations, SSRT_SETTING(MaxIterations));

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
    int maxUsedLevel = minMipLevel;
    ScreenSpaceTracingDebug debug;
    ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);
    if (input.debug)
        FillScreenSpaceRaymarchingPreLoopDebug(startPositionSS, debug);
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
        if (input.debug)
            FillScreenSpaceRaymarchingPreIterationDebug(iteration, currentLevel, debug);
#endif

        // Go down in HiZ levels by default
        int mipLevelDelta = -1;

        // Sampled as 1/Z so it interpolate properly in screen space.
        invHiZDepth = LoadInvDepth(positionSS.xy, currentLevel);
        intersectionKind = 0;

        if (IsPositionAboveDepth(positionSS.z, invHiZDepth))
        {
            float3 candidatePositionSS = IntersectDepthPlane(positionSS, raySS, invHiZDepth);

            intersectionKind = 1;

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

                intersectionKind = 2;

                // Go up a level to go faster
                mipLevelDelta = 1;
            }

            positionSS = candidatePositionSS;
        }

        currentLevel = min(currentLevel + mipLevelDelta, maxMipLevel);
        
#ifdef DEBUG_DISPLAY
        maxUsedLevel = max(maxUsedLevel, currentLevel);
        if (input.debug)
            FillScreenSpaceRaymarchingPostIterationDebug(
                iteration,
                cellSize,
                positionSS,
                invHiZDepth,
                intersectionKind,
                debug
            );
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

    if (hit.linearDepth > (1 / invHiZDepth) + SSRT_SETTING(DepthSuccessBias))
        hitSuccessful = false;

#ifdef DEBUG_DISPLAY
    if (input.debug)
    {
        FillScreenSpaceRaymarchingPostLoopDebug(
            maxUsedLevel,
            iteration,
            raySS,
            hitSuccessful,
            hit,
            debug
        );
        FillScreenSpaceRaymarchingHitDebug(
            bufferSize, 
            input.rayDirVS, 
            raySS, 
            startPositionSS, 
            hitSuccessful, 
            iteration, 
            SSRT_SETTING(MaxIterations), 
            maxMipLevel, 
            maxUsedLevel,
            intersectionKind, 
            hit
        );
        _DebugScreenSpaceTracingData[0] = debug;
    }
#endif

    return hitSuccessful;
}

#undef SSRT_SETTING
#endif