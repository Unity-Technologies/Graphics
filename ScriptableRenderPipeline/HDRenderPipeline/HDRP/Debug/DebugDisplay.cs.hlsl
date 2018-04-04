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
    uint startPositionSSX;
    uint startPositionSSY;
    uint cellSizeW;
    uint cellSizeH;
    float3 positionSS;
    float startLinearDepth;
    uint level;
    uint levelMax;
    uint iteration;
    uint iterationMax;
    bool hitSuccess;
    float hitLinearDepth;
    float2 hitPositionSS;
    float hiZLinearDepth;
    float3 raySS;
    uint intersectionKind;
    float resultHitDepth;
    uint endPositionSSX;
    uint endPositionSSY;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.ScreenSpaceTracingDebug
//
uint GetStartPositionSSX(ScreenSpaceTracingDebug value)
{
	return value.startPositionSSX;
}
uint GetStartPositionSSY(ScreenSpaceTracingDebug value)
{
	return value.startPositionSSY;
}
uint GetCellSizeW(ScreenSpaceTracingDebug value)
{
	return value.cellSizeW;
}
uint GetCellSizeH(ScreenSpaceTracingDebug value)
{
	return value.cellSizeH;
}
float3 GetPositionSS(ScreenSpaceTracingDebug value)
{
	return value.positionSS;
}
float GetStartLinearDepth(ScreenSpaceTracingDebug value)
{
	return value.startLinearDepth;
}
uint GetLevel(ScreenSpaceTracingDebug value)
{
	return value.level;
}
uint GetLevelMax(ScreenSpaceTracingDebug value)
{
	return value.levelMax;
}
uint GetIteration(ScreenSpaceTracingDebug value)
{
	return value.iteration;
}
uint GetIterationMax(ScreenSpaceTracingDebug value)
{
	return value.iterationMax;
}
bool GetHitSuccess(ScreenSpaceTracingDebug value)
{
	return value.hitSuccess;
}
float GetHitLinearDepth(ScreenSpaceTracingDebug value)
{
	return value.hitLinearDepth;
}
float2 GetHitPositionSS(ScreenSpaceTracingDebug value)
{
	return value.hitPositionSS;
}
float GetHiZLinearDepth(ScreenSpaceTracingDebug value)
{
	return value.hiZLinearDepth;
}
float3 GetRaySS(ScreenSpaceTracingDebug value)
{
	return value.raySS;
}
uint GetIntersectionKind(ScreenSpaceTracingDebug value)
{
	return value.intersectionKind;
}
float GetResultHitDepth(ScreenSpaceTracingDebug value)
{
	return value.resultHitDepth;
}
uint GetEndPositionSSX(ScreenSpaceTracingDebug value)
{
	return value.endPositionSSX;
}
uint GetEndPositionSSY(ScreenSpaceTracingDebug value)
{
	return value.endPositionSSY;
}


#endif
