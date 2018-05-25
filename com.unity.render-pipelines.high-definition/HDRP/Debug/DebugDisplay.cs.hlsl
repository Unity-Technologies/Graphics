//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DEBUGDISPLAY_CS_HLSL
#define DEBUGDISPLAY_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.FullScreenDebugMode:  static fields
//
#define FULLSCREENDEBUGMODE_NONE (0)
#define FULLSCREENDEBUGMODE_MIN_LIGHTING_FULL_SCREEN_DEBUG (1)
#define FULLSCREENDEBUGMODE_SSAO (2)
#define FULLSCREENDEBUGMODE_DEFERRED_SHADOWS (3)
#define FULLSCREENDEBUGMODE_PRE_REFRACTION_COLOR_PYRAMID (4)
#define FULLSCREENDEBUGMODE_DEPTH_PYRAMID (5)
#define FULLSCREENDEBUGMODE_FINAL_COLOR_PYRAMID (6)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_TRACING (7)
#define FULLSCREENDEBUGMODE_MAX_LIGHTING_FULL_SCREEN_DEBUG (8)
#define FULLSCREENDEBUGMODE_MIN_RENDERING_FULL_SCREEN_DEBUG (9)
#define FULLSCREENDEBUGMODE_MOTION_VECTORS (10)
#define FULLSCREENDEBUGMODE_NAN_TRACKER (11)
#define FULLSCREENDEBUGMODE_MAX_RENDERING_FULL_SCREEN_DEBUG (12)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.ScreenSpaceTracingDebug
// PackingRules = Exact
struct ScreenSpaceTracingDebug
{
    int tracingModel;
    uint loopStartPositionSSX;
    uint loopStartPositionSSY;
    float loopStartLinearDepth;
    float3 loopRayDirectionSS;
    uint loopMipLevelMax;
    uint loopIterationMax;
    float3 iterationPositionSS;
    uint iterationMipLevel;
    uint iteration;
    float iterationLinearDepthBufferMin;
    float iterationLinearDepthBufferMax;
    float iterationLinearDepthBufferMinThickness;
    int iterationIntersectionKind;
    uint iterationCellSizeW;
    uint iterationCellSizeH;
    int proxyShapeType;
    float projectionDistance;
    int endHitSuccess;
    float endLinearDepth;
    uint endPositionSSX;
    uint endPositionSSY;
    float endHitWeight;
    float3 lightingSampledColor;
    float3 lightingSpecularFGD;
    float lightingWeight;
    float2 padding;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.ScreenSpaceTracingDebug
//
int GetTracingModel(ScreenSpaceTracingDebug value)
{
    return value.tracingModel;
}
uint GetLoopStartPositionSSX(ScreenSpaceTracingDebug value)
{
    return value.loopStartPositionSSX;
}
uint GetLoopStartPositionSSY(ScreenSpaceTracingDebug value)
{
    return value.loopStartPositionSSY;
}
float GetLoopStartLinearDepth(ScreenSpaceTracingDebug value)
{
    return value.loopStartLinearDepth;
}
float3 GetLoopRayDirectionSS(ScreenSpaceTracingDebug value)
{
    return value.loopRayDirectionSS;
}
uint GetLoopMipLevelMax(ScreenSpaceTracingDebug value)
{
    return value.loopMipLevelMax;
}
uint GetLoopIterationMax(ScreenSpaceTracingDebug value)
{
    return value.loopIterationMax;
}
float3 GetIterationPositionSS(ScreenSpaceTracingDebug value)
{
    return value.iterationPositionSS;
}
uint GetIterationMipLevel(ScreenSpaceTracingDebug value)
{
    return value.iterationMipLevel;
}
uint GetIteration(ScreenSpaceTracingDebug value)
{
    return value.iteration;
}
float GetIterationLinearDepthBufferMin(ScreenSpaceTracingDebug value)
{
    return value.iterationLinearDepthBufferMin;
}
float GetIterationLinearDepthBufferMax(ScreenSpaceTracingDebug value)
{
    return value.iterationLinearDepthBufferMax;
}
float GetIterationLinearDepthBufferMinThickness(ScreenSpaceTracingDebug value)
{
    return value.iterationLinearDepthBufferMinThickness;
}
int GetIterationIntersectionKind(ScreenSpaceTracingDebug value)
{
    return value.iterationIntersectionKind;
}
uint GetIterationCellSizeW(ScreenSpaceTracingDebug value)
{
    return value.iterationCellSizeW;
}
uint GetIterationCellSizeH(ScreenSpaceTracingDebug value)
{
    return value.iterationCellSizeH;
}
int GetProxyShapeType(ScreenSpaceTracingDebug value)
{
    return value.proxyShapeType;
}
float GetProjectionDistance(ScreenSpaceTracingDebug value)
{
    return value.projectionDistance;
}
int GetEndHitSuccess(ScreenSpaceTracingDebug value)
{
    return value.endHitSuccess;
}
float GetEndLinearDepth(ScreenSpaceTracingDebug value)
{
    return value.endLinearDepth;
}
uint GetEndPositionSSX(ScreenSpaceTracingDebug value)
{
    return value.endPositionSSX;
}
uint GetEndPositionSSY(ScreenSpaceTracingDebug value)
{
    return value.endPositionSSY;
}
float GetEndHitWeight(ScreenSpaceTracingDebug value)
{
    return value.endHitWeight;
}
float3 GetLightingSampledColor(ScreenSpaceTracingDebug value)
{
    return value.lightingSampledColor;
}
float3 GetLightingSpecularFGD(ScreenSpaceTracingDebug value)
{
    return value.lightingSpecularFGD;
}
float GetLightingWeight(ScreenSpaceTracingDebug value)
{
    return value.lightingWeight;
}
float2 GetPadding(ScreenSpaceTracingDebug value)
{
    return value.padding;
}


#endif
