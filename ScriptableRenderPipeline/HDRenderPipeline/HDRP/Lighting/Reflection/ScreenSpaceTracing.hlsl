#ifndef UNITY_SCREEN_SPACE_TRACING_INCLUDED
#define UNITY_SCREEN_SPACE_TRACING_INCLUDED

// -------------------------------------------------
// Algorithm uniform parameters
// -------------------------------------------------

CBUFFER_START(UnityScreenSpaceRaymarching)
int _SSRayMinLevel;
int _SSRayMaxLevel;
int _SSRayMaxIterations;
float _SSRayDepthSuccessBias;
CBUFFER_END

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
    out float3 raySS)
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
    float2 crossOffset)
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
    float3 raySS,
    float3 startPositionSS,
    bool hitSuccessful,
    int iteration,
    int maxIterations,
    int maxUsedLevel,
    int maxMipLevel,
    int intersectionKind,
    inout ScreenSpaceRayHit hit)
{
    float3 debugOutput = float3(0, 0, 0);
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFRACTION)
    {
        switch (_DebugLightingSubMode)
        {
        case DEBUGSCREENSPACETRACING_POSITION_NDC:
            debugOutput =  float3(float2(startPositionSS.xy) / bufferSize, 0);
            break;
        case DEBUGSCREENSPACETRACING_DIR_WS:
            debugOutput =  rayDirWS * 0.5 + 0.5;
            break;
        case DEBUGSCREENSPACETRACING_DIR_NDC:
            debugOutput =  float3(raySS.xy * 0.5 + 0.5, frac(0.1 / raySS.z));
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
    }
    hit.debugOutput = debugOutput;
}

void FillScreenSpaceRaymarchingPreLoopDebug(
    float3 startPositionSS,
    inout ScreenSpaceTracingDebug debug)
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
    inout ScreenSpaceTracingDebug debug)
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
    inout ScreenSpaceTracingDebug debug)
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
    inout ScreenSpaceTracingDebug debug)
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

// -------------------------------------------------
// Algorithm: Proxy raycast
// -------------------------------------------------

struct ScreenSpaceProxyRaycastInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)

#ifdef DEBUG_DISPLAY
    bool writeStepDebug;
#endif
};


bool ScreenSpaceEstimateRaycast(
    ScreenSpaceEstimateRaycastInput input,
    out ScreenSpaceRayHit hit)
{
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    return false;
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------

// Based on Yasin Uludag, 2014. "Hi-Z Screen-Space Cone-Traced Reflections", GPU Pro5: Advanced Rendering Techniques

struct ScreenSpaceHiZRaymarchInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    uint maxIterations;          // Number of iterations before failing

#ifdef DEBUG_DISPLAY
    bool writeStepDebug;
#endif
};

bool ScreenSpaceHiZRaymarch(
    ScreenSpaceHiZRaymarchInput input,
    out ScreenSpaceRayHit hit)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = true;
    uint iteration = 0u;
    int minMipLevel = max(_SSRayMinLevel, 0);
    int maxMipLevel = min(_SSRayMaxLevel, int(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = min(input.maxIterations, _SSRayMaxIterations);

    float3 startPositionSS;
    float3 raySS;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS);

#ifdef DEBUG_DISPLAY
    int maxUsedLevel = minMipLevel;
    ScreenSpaceTracingDebug debug;
    ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);
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
        FillScreenSpaceRaymarchingPostIterationDebug(
            iteration,
            cellSize,
            positionSS,
            invHiZDepth,
            intersectionKind,
            debug);
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

    if (hit.linearDepth > (1 / invHiZDepth) + _SSRayDepthSuccessBias)
        hitSuccessful = false;

#ifdef DEBUG_DISPLAY
    FillScreenSpaceRaymarchingPostLoopDebug(
        maxUsedLevel,
        iteration,
        raySS,
        hitSuccessful,
        hit,
        debug);
    FillScreenSpaceRaymarchingHitDebug(
        bufferSize, 
        input.rayDirVS, 
        raySS, 
        startPositionSS, 
        hitSuccessful, 
        iteration, 
        _SSRayMaxIterations, 
        maxMipLevel, 
        maxUsedLevel,
        intersectionKind, 
        hit);
    if (input.writeStepDebug)
        _DebugScreenSpaceTracingData[0] = debug;
#endif

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: Linear raymarching
// -------------------------------------------------
// Based on DDA (https://en.wikipedia.org/wiki/Digital_differential_analyzer_(graphics_algorithm))
// Based on Morgan McGuire and Michael Mara, 2014. "Efficient GPU Screen-Space Ray Tracing", Journal of Computer Graphics Techniques (JCGT), 235-256

struct ScreenSpaceLinearRaymarchInput
{
    float3 rayOriginVS;         // Ray origin (VS)
    float3 rayDirVS;            // Ray direction (VS)
    float4x4 projectionMatrix;  // Projection matrix of the camera
    uint maxIterations;         // Number of iterations before failing

#ifdef DEBUG_DISPLAY
    bool writeStepDebug;
#endif
};

// Basically, perform a raycast with DDA technique on a specific mip level of the Depth pyramid.
bool ScreenSpaceLinearRaymarch(
    ScreenSpaceLinearRaymarchInput input,
    out ScreenSpaceRayHit hit)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = true;
    uint iteration = 0u;
    int level = clamp(_SSRayMinLevel, 0, int(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = min(input.maxIterations, _SSRayMaxIterations);

    float3 startPositionSS;
    float3 raySS;
    CalculateRaySS(
        input.rayOriginVS,
        input.rayDirVS,
        input.projectionMatrix,
        bufferSize,
        startPositionSS,
        raySS);

#ifdef DEBUG_DISPLAY
    ScreenSpaceTracingDebug debug;
    ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);
    FillScreenSpaceRaymarchingPreLoopDebug(startPositionSS, debug);
#endif

    float maxAbsAxis = max(abs(raySS.x), abs(raySS.y));
    // No need to raymarch if the ray is along camera's foward
    if (maxAbsAxis < 1E-7)
    {
        hit.distanceSS = 1 / startPositionSS.z;
        hit.linearDepth = 1 / startPositionSS.z;
        hit.positionSS = uint2(startPositionSS.xy);
    }
    else
    {
        // DDA step
        raySS /= max(abs(raySS.x), abs(raySS.y));
        raySS *= _SSRayMinLevel;

        float distanceStepSS = length(raySS.xy);

        float3 positionSS = startPositionSS;
        // TODO: We should have a for loop from the starting point to the far/near plane
        while (iteration < maxIterations)
        {
#ifdef DEBUG_DISPLAY
            FillScreenSpaceRaymarchingPreIterationDebug(iteration, 0, debug);
#endif

            positionSS += raySS;
            hit.distanceSS += distanceStepSS;
            float invHiZDepth = LoadInvDepth(positionSS.xy, _SSRayMinLevel);

#ifdef DEBUG_DISPLAY
            FillScreenSpaceRaymarchingPostIterationDebug(
                iteration,
                uint2(0, 0),
                positionSS,
                invHiZDepth,
                0,
                debug);
#endif

            if (!IsPositionAboveDepth(positionSS.z, invHiZDepth))
            {
                if (1 / positionSS.z > (1 / invHiZDepth + _SSRayDepthSuccessBias))
                    hitSuccessful = false;
                break;
            }

            // Check if we are out of the buffer
            if (any(int2(positionSS.xy) > int2(bufferSize))
                || any(positionSS.xy < 0))
            {
                hitSuccessful = false;
                break;
            }

            ++iteration;
        }

        hit.linearDepth = 1 / positionSS.z;
        hit.positionNDC = float2(positionSS.xy) / float2(bufferSize);
        hit.positionSS = uint2(positionSS.xy);
    }

#ifdef DEBUG_DISPLAY
    FillScreenSpaceRaymarchingPostLoopDebug(
        0,
        iteration,
        raySS,
        hitSuccessful,
        hit,
        debug);
    FillScreenSpaceRaymarchingHitDebug(
        bufferSize, 
        input.rayDirVS, 
        raySS,
        startPositionSS, 
        hitSuccessful, 
        iteration, 
        _SSRayMaxIterations, 
        0, 
        0,
        0,
        hit);
    if (input.writeStepDebug)
        _DebugScreenSpaceTracingData[0] = debug;
#endif

    return hitSuccessful;
}

#endif