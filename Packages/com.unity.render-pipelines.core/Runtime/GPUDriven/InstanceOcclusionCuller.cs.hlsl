//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef INSTANCEOCCLUSIONCULLER_CS_HLSL
#define INSTANCEOCCLUSIONCULLER_CS_HLSL
//
// UnityEngine.Rendering.InstanceOcclusionTestDebugCounter:  static fields
//
#define INSTANCEOCCLUSIONTESTDEBUGCOUNTER_OCCLUDED (0)
#define INSTANCEOCCLUSIONTESTDEBUGCOUNTER_NOT_OCCLUDED (1)
#define INSTANCEOCCLUSIONTESTDEBUGCOUNTER_COUNT (2)

// Generated from UnityEngine.Rendering.IndirectDrawInfo
// PackingRules = Exact
struct IndirectDrawInfo
{
    uint indexCount;
    uint firstIndex;
    uint baseVertex;
    uint firstInstanceGlobalIndex;
    uint maxInstanceCount;
};

// Generated from UnityEngine.Rendering.IndirectInstanceInfo
// PackingRules = Exact
struct IndirectInstanceInfo
{
    int drawOffsetAndSplitMask;
    int instanceIndexAndCrossFade;
};


#endif
