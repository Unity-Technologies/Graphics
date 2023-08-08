//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef GPUINLINEDEBUGDRAWER_CS_HLSL
#define GPUINLINEDEBUGDRAWER_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.GPUInlineDebugDrawer+GPUInlineDebugDrawerParams:  static fields
//
#define GPUINLINEDEBUGDRAWERPARAMS_MAX_LINES (4096)
#define GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER (256)

// Generated from UnityEngine.Rendering.HighDefinition.GPUInlineDebugDrawerLine
// PackingRules = Exact
struct GPUInlineDebugDrawerLine
{
    float4 start;
    float4 end;
    float4 startColor; // x: r y: g z: b w: a 
    float4 endColor; // x: r y: g z: b w: a 
};


#endif
