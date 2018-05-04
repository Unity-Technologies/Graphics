#ifndef UNITY_SCREEN_SPACE_TRACING_INCLUDED
#define UNITY_SCREEN_SPACE_TRACING_INCLUDED

// How this file works:
// This file is separated in two sections: 1. Library, 2. Constant Buffer Specific Signatures
//
// 1. Library
//   This section contains all function and structures for the Screen Space Tracing.
//
// 2. Constant Buffer Specific Signatures
//   This section defines signatures that will use specifics constant buffers.
// Thus you can use the Screen Space Tracing library with different settings.
// It can be usefull to use it for both reflection and refraction but with different settings' sets.
//
//
// To use this file:
// 1. Define the macro SSRTID
// 2. Include the file
// 3. Undef the macro SSRTID
//
//
// Example for reflection:
// #define SSRTID Reflection
// #include "ScreenSpaceTracing.hlsl"
// #undef SSRTID
//
// Use library here, like ScreenSpaceProxyRaycastReflection(...)



// #################################################
// Screen Space Tracing Library
// #################################################

// -------------------------------------------------
// Algorithm uniform parameters
// -------------------------------------------------
const float DepthPlaneBias = 1E-5;

// -------------------------------------------------
// Output
// -------------------------------------------------

struct ScreenSpaceRayHit
{
    uint2 positionSS;           // Position of the hit point (SS)
    float2 positionNDC;         // Position of the hit point (NDC)
    float linearDepth;          // Linear depth of the hit point

#ifdef DEBUG_DISPLAY
    float3 debugOutput;
#endif
};

struct ScreenSpaceRaymarchInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

struct ScreenSpaceProxyRaycastInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    EnvLightData proxyData;     // Proxy to use for raycasting

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

// -------------------------------------------------
// Utilities
// -------------------------------------------------

// Calculate the ray origin and direction in SS
void CalculateRaySS(
    float3 rayOriginWS,             // Ray origin (World Space)
    float3 rayDirWS,                // Ray direction (World Space)
    uint2 bufferSize,               // Texture size of screen buffers
    out float3 positionSS,          // (x, y, 1/linearDepth)
    out float3 raySS,               // (dx, dy, d(1/linearDepth))
    out float rayEndDepth           // Linear depth of the end point used to calculate raySS
)
{
    float3 positionWS = rayOriginWS;
    float3 rayEndWS = rayOriginWS + rayDirWS * 10;

    float4 positionCS = ComputeClipSpacePosition(positionWS, GetWorldToHClipMatrix());
    float4 rayEndCS = ComputeClipSpacePosition(rayEndWS, GetWorldToHClipMatrix());

    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, GetWorldToHClipMatrix());
    float2 rayEndNDC = ComputeNormalizedDeviceCoordinates(rayEndWS, GetWorldToHClipMatrix());
    rayEndDepth = rayEndCS.w;

    float3 rayStartSS = float3(
        positionNDC.xy * bufferSize,
        1.0 / positionCS.w); // Screen space depth interpolate properly in 1/z

    float3 rayEndSS = float3(
        rayEndNDC.xy * bufferSize,
        1.0 / rayEndDepth); // Screen space depth interpolate properly in 1/z

    positionSS = rayStartSS;
    raySS = rayEndSS - rayStartSS;
}

// Check whether the depth of the ray is above the sampled depth
// Arguments are inversed linear depth
bool IsPositionAboveDepth(float invRayDepth, float invLinearDepth)
{
    // as depth is inverted, we must invert the check as well
    // rayZ > HiZ <=> 1/rayZ < 1/HiZ
    return invRayDepth > invLinearDepth;
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
    float3 positionSS,              // Ray Origin (Screen Space, 1/LinearDepth)
    float3 raySS,                   // Ray Direction (Screen Space, 1/LinearDepth)
    float2 invRaySS,                // 1/RayDirection
    int2 cellId,                    // (Row, Colum) of the cell
    uint2 cellSize,                 // Size of the cell in pixel
    int2 cellPlanes,                // Planes to intersect (one of (0,0), (1, 0), (0, 1), (1, 1))
    float2 crossOffset              // Offset to use to ensure cell boundary crossing
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
    int tracingModel,
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
        hit.debugOutput =  GetIndexColor(hitSuccessful ? 1 : 2);
        break;
    case DEBUGSCREENSPACETRACING_TRACING_MODEL:
        hit.debugOutput =  GetIndexColor(tracingModel);
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

// -------------------------------------------------
// Algorithms
// -------------------------------------------------

// -------------------------------------------------
// Algorithm: Linear Raymarching
// -------------------------------------------------
// Based on Digital Differential Analyzer and Morgan McGuire's Screen Space Ray Tracing (http://casual-effects.blogspot.fr/2014/08/screen-space-ray-tracing.html)
//
// Linear raymarching algorithm with precomputed properties
// -------------------------------------------------
bool ScreenSpaceLinearRaymarch(
    ScreenSpaceRaymarchInput input,
    // Settings
    int settingRayLevel,                    // Mip level to use to ray march depth buffer
    uint settingsRayMaxIterations,          // Maximum number of iterations (= max number of depth samples)
    float settingsRayDepthSuccessBias,      // Bias to use when trying to detect whenever we raymarch behind a surface
    int settingsDebuggedAlgorithm,          // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Precomputed properties
    float3 startPositionSS,                 // Start position in Screen Space (x in pixel, y in pixel, z = 1/linearDepth)
    float3 raySS,                           // Ray direction in Screen Space (dx in pixel, dy in pixel, z = 1/endPointLinearDepth - 1/startPointLinearDepth)
    float rayEndDepth,                      // Linear depth of the end point used to calculate raySS.
    uint2 bufferSize,                       // Texture size of screen buffers
    // Out
    out ScreenSpaceRayHit hit,
    out uint iteration
)
{
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = false;
    iteration = 0u;
    int mipLevel = min(max(settingRayLevel, 0), int(_DepthPyramidScale.z));
    uint maxIterations = settingsRayMaxIterations;

    float3 positionSS = startPositionSS;
    raySS /= max(abs(raySS.x), abs(raySS.y));
    raySS *= 1 << mipLevel;

    // Offset by a texel
    positionSS += raySS * 1.0;

#ifdef DEBUG_DISPLAY
    float3 debugIterationPositionSS = positionSS;
    float debugIterationLinearDepthBuffer = 0;
    uint debugIteration = iteration;
#endif

    float invLinearDepth = 0;

    for (iteration = 0u; iteration < maxIterations; ++iteration)
    {
        positionSS += raySS;

        // Sampled as 1/Z so it interpolate properly in screen space.
        invLinearDepth = LoadInvDepth(positionSS.xy, mipLevel);

#ifdef DEBUG_DISPLAY
        // Fetch post iteration debug values
        if (input.debug && _DebugStep >= int(iteration))
        {
            debugIterationPositionSS = positionSS;
            debugIterationLinearDepthBuffer = 1 / invLinearDepth;
            debugIteration = iteration;
        }
#endif

        if (!IsPositionAboveDepth(positionSS.z, invLinearDepth))
        {
            hitSuccessful = true;
            break;
        }

        // Check if we are out of the buffer
        if (any(int2(positionSS.xy) > int2(bufferSize))
            || any(positionSS.xy < 0)
            )
        {
            hitSuccessful = false;
            break;
        }
    }

    if (iteration >= maxIterations)
        hitSuccessful = false;

    hit.linearDepth = 1 / positionSS.z;
    hit.positionNDC = float2(positionSS.xy) / float2(bufferSize);
    hit.positionSS = uint2(positionSS.xy);

    if (hit.linearDepth > (1 / invLinearDepth) + settingsRayDepthSuccessBias)
        hitSuccessful = false;

#ifdef DEBUG_DISPLAY
    DebugComputeCommonOutput(input.rayDirWS, hitSuccessful, PROJECTIONMODEL_LINEAR, hit);

    if (input.debug 
        && _DebugScreenSpaceTracingData[0].tracingModel == -1
        && settingsDebuggedAlgorithm == PROJECTIONMODEL_LINEAR
    )
    {
        // Build debug structure
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);

        debug.tracingModel                  = PROJECTIONMODEL_LINEAR;
        debug.loopStartPositionSSX          = uint(startPositionSS.x);
        debug.loopStartPositionSSY          = uint(startPositionSS.y);
        debug.loopStartLinearDepth          = 1 / startPositionSS.z;
        debug.loopRayDirectionSS            = raySS;
        debug.loopIterationMax              = iteration;
        debug.iterationPositionSS           = debugIterationPositionSS;
        debug.iterationMipLevel             = mipLevel;
        debug.iteration                     = debugIteration;
        debug.iterationLinearDepthBuffer    = debugIterationLinearDepthBuffer;
        debug.endHitSuccess                 = hitSuccessful;
        debug.endLinearDepth                = hit.linearDepth;
        debug.endPositionSSX                = hit.positionSS.x;
        debug.endPositionSSY                = hit.positionSS.y;
        debug.iterationCellSizeW            = 1 << mipLevel;
        debug.iterationCellSizeH            = 1 << mipLevel;

        _DebugScreenSpaceTracingData[0] = debug;
    }
#endif

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: Scene Proxy Raycasting
// -------------------------------------------------
// We perform a raycast against a proxy volume that approximate the current scene.
// Is is a simple shape (Sphere, Box).
// -------------------------------------------------
bool ScreenSpaceProxyRaycast(
    ScreenSpaceProxyRaycastInput input,
    // Settings
    int settingsDebuggedAlgorithm,             // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Out
    out ScreenSpaceRayHit hit
)
{
    // Initialize loop
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

    bool hitSuccessful      = hitLinearDepth > 0;       // Negative means that the hit is behind the camera

#ifdef DEBUG_DISPLAY
    DebugComputeCommonOutput(input.rayDirWS, hitSuccessful, PROJECTIONMODEL_PROXY, hit);
    
    if (input.debug 
        && _DebugScreenSpaceTracingData[0].tracingModel == -1
        && settingsDebuggedAlgorithm == PROJECTIONMODEL_PROXY
    )
    {
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);

        float2 rayOriginNDC         = ComputeNormalizedDeviceCoordinates(input.rayOriginWS, GetWorldToHClipMatrix());
        uint2 rayOriginSS           = uint2(rayOriginNDC * _ScreenSize.xy);

        debug.tracingModel          = PROJECTIONMODEL_PROXY;
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
// Algorithm: Linear Raymarching And Scene Proxy Raycasting
// -------------------------------------------------
// Perform a linear raymarching for close hit detection and fallback on proxy raycasting
// -------------------------------------------------
bool ScreenSpaceLinearProxyRaycast(
    ScreenSpaceProxyRaycastInput input,
    // Settings (linear)
    int settingRayLevel,                    // Mip level to use to ray march depth buffer
    uint settingsRayMaxIterations,          // Maximum number of iterations (= max number of depth samples)
    float settingsRayDepthSuccessBias,      // Bias to use when trying to detect whenever we raymarch behind a surface
    // Settings (common)
    int settingsDebuggedAlgorithm,          // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Out
    out ScreenSpaceRayHit hit
)
{
    // Perform linear raymarch
    ScreenSpaceRaymarchInput inputLinear;
    inputLinear.rayOriginWS = input.rayOriginWS;
    inputLinear.rayDirWS = input.rayDirWS;
#ifdef DEBUG_DISPLAY
    inputLinear.debug = input.debug;
#endif

    uint2 bufferSize = uint2(_DepthPyramidSize.xy);

    // Compute properties for linear raymarch
    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

    uint iteration;
    bool hitSuccessful = ScreenSpaceLinearRaymarch(
        inputLinear,
        // Settings
        settingRayLevel,
        settingsRayMaxIterations,
        settingsRayDepthSuccessBias,
        settingsDebuggedAlgorithm,
        // Precomputed properties
        startPositionSS,
        raySS,
        rayEndDepth,
        bufferSize,
        // Out
        hit,
        iteration
    );

    if (!hitSuccessful)
    {
        hitSuccessful = ScreenSpaceProxyRaycast(
            input,
            // Settings
            settingsDebuggedAlgorithm,
            // Out
            hit
        );
    }

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------
// Based on Yasin Uludag, 2014. "Hi-Z Screen-Space Cone-Traced Reflections", GPU Pro5: Advanced Rendering Techniques
//
// NB: We perform first a linear raymarch to handle close hits, then we perform the actual HiZ raymarching.
// We do this for two reasons:
//  - It is cheaper in case of close hit than starting with HiZ
//  - It will start the HiZ algorithm with an offset, preventing false positive hit at ray origin.
//
// NB: Maximum of depth samples = settingsRayMaxIterations + settingsRayMaxLinearIterations
// -------------------------------------------------
bool ScreenSpaceHiZRaymarch(
    ScreenSpaceRaymarchInput input,
    // Settings
    uint settingsRayLevel,                          // Mip level to use for linear ray marching in depth buffer
    uint settingsRayMinLevel,                       // Minimum mip level to use for ray marching the depth buffer in HiZ
    uint settingsRayMaxLevel,                       // Maximum mip level to use for ray marching the depth buffer in HiZ
    uint settingsRayMaxIterations,                  // Maximum number of iteration for the HiZ raymarching (= number of depth sample for HiZ)
    uint settingsRayMaxLinearIterations,            // Maximum number of iteration for the linear raymarching (= number of depth sample for linear)
    float settingsRayDepthSuccessBias,              // Bias to use when trying to detect whenever we raymarch behind a surface
    int settingsDebuggedAlgorithm,                  // currently debugged algorithm (see PROJECTIONMODEL defines)
    // out
    out ScreenSpaceRayHit hit
)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = false;
    uint iteration = 0u;
    int minMipLevel = max(settingsRayMinLevel, 0u);
    int maxMipLevel = min(settingsRayMaxLevel, uint(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = settingsRayMaxIterations;

    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

    // We perform first a linear raymarching
    // It is more performant for short distance than HiZ raymarching
    if (ScreenSpaceLinearRaymarch(
            input,
            // settings
            settingsRayLevel,
            settingsRayMaxLinearIterations,
            settingsRayDepthSuccessBias,
            settingsDebuggedAlgorithm,
            // precomputed
            startPositionSS,
            raySS,
            rayEndDepth,
            bufferSize,
            // out
            hit,
            iteration
        ))
        return true;
    
    startPositionSS = float3(hit.positionSS, 1 / hit.linearDepth);

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

    iteration = 0u;
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
        hitSuccessful = true;
        if (iteration >= maxIterations)
        {
            hitSuccessful = false;
            break;
        }

        cellCount = bufferSize >> currentLevel;
        cellSize = uint2(1, 1) << currentLevel;


#ifdef DEBUG_DISPLAY
        // Fetch pre iteration debug values
        if (input.debug && _DebugStep >= int(iteration))
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
        if (input.debug && _DebugStep >= int(iteration))
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

    if (hit.linearDepth > (1 / invHiZDepth) + settingsRayDepthSuccessBias)
        hitSuccessful = false;

#ifdef DEBUG_DISPLAY
    DebugComputeCommonOutput(input.rayDirWS, hitSuccessful, PROJECTIONMODEL_HI_Z, hit);
    DebugComputeHiZOutput(
        iteration,
        startPositionSS,
        raySS,
        settingsRayMaxIterations,
        debugLoopMipMaxUsedLevel,
        maxMipLevel,
        intersectionKind,
        hit
    );

    if (input.debug 
        && _DebugScreenSpaceTracingData[0].tracingModel == -1
        && settingsDebuggedAlgorithm == PROJECTIONMODEL_HI_Z
    )
    {
        // Build debug structure
        ScreenSpaceTracingDebug debug;
        ZERO_INITIALIZE(ScreenSpaceTracingDebug, debug);

        debug.tracingModel                  = PROJECTIONMODEL_HI_Z;
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
#endif





// #################################################
// Screen Space Tracing CB Specific Signatures
// #################################################

#ifdef SSRTID
// -------------------------------------------------
// Macros
// -------------------------------------------------
#define SSRT_SETTING(name, SSRTID) _SS ## SSRTID ## name

// -------------------------------------------------
// Constant buffers
// -------------------------------------------------
CBUFFER_START(MERGE_NAME(UnityScreenSpaceRaymarching, SSRTID))
int SSRT_SETTING(RayLevel, SSRTID);
int SSRT_SETTING(RayMinLevel, SSRTID);
int SSRT_SETTING(RayMaxLevel, SSRTID);
int SSRT_SETTING(RayMaxLinearIterations, SSRTID);
int SSRT_SETTING(RayMaxIterations, SSRTID);
float SSRT_SETTING(RayDepthSuccessBias, SSRTID);

#ifdef DEBUG_DISPLAY
int SSRT_SETTING(DebuggedAlgorithm, SSRTID);
#endif
CBUFFER_END

// -------------------------------------------------
// Algorithm: Linear Raymarching
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceLinearRaymarch, SSRTID)(
    ScreenSpaceRaymarchInput input,
    out ScreenSpaceRayHit hit
)
{
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

    uint iteration;
    return ScreenSpaceLinearRaymarch(
        input,
        // settings
        SSRT_SETTING(RayLevel, SSRTID),
        SSRT_SETTING(RayMaxIterations, SSRTID),
        SSRT_SETTING(RayDepthSuccessBias, SSRTID),
#ifdef DEBUG_DISPLAY
        SSRT_SETTING(DebuggedAlgorithm, SSRTID),
#else
        PROJECTIONMODEL_NONE,
#endif
        // precomputed properties
        startPositionSS,
        raySS,
        rayEndDepth,
        bufferSize,
        // out
        hit,
        iteration
    );
}

// -------------------------------------------------
// Algorithm: Scene Proxy Raycasting
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceProxyRaycast, SSRTID)(
    ScreenSpaceProxyRaycastInput input,
    out ScreenSpaceRayHit hit
)
{
    return ScreenSpaceProxyRaycast(
        input,
        // Settings
#if DEBUG_DISPLAY
        int(SSRT_SETTING(DebuggedAlgorithm, SSRTID)),
#else
        int(PROJECTIONMODEL_NONE),
#endif
        // Out
        hit
    );
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceHiZRaymarch, SSRTID)(
    ScreenSpaceRaymarchInput input,
    out ScreenSpaceRayHit hit
)
{
    return ScreenSpaceHiZRaymarch(
        input,
        // Settings
        SSRT_SETTING(RayLevel, SSRTID),
        SSRT_SETTING(RayMinLevel, SSRTID),
        SSRT_SETTING(RayMaxLevel, SSRTID),
        SSRT_SETTING(RayMaxIterations, SSRTID),
        SSRT_SETTING(RayMaxLinearIterations, SSRTID),
        SSRT_SETTING(RayDepthSuccessBias, SSRTID),
#ifdef DEBUG_DISPLAY
        SSRT_SETTING(DebuggedAlgorithm, SSRTID),
#else
        PROJECTIONMODEL_NONE,
#endif
        // out
        hit
    );
}

// -------------------------------------------------
// Cleaning
// -------------------------------------------------
#undef SSRT_SETTING
#endif