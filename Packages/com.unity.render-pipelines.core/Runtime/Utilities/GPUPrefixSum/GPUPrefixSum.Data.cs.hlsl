//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef GPUPREFIXSUM_DATA_CS_HLSL
#define GPUPREFIXSUM_DATA_CS_HLSL
//
// UnityEngine.Rendering.GPUPrefixSum+ShaderDefs:  static fields
//
#define GROUP_SIZE (128)
#define ARGS_BUFFER_STRIDE (16)
#define ARGS_BUFFER_UPPER (0)
#define ARGS_BUFFER_LOWER (8)

// Generated from UnityEngine.Rendering.GPUPrefixSum+LevelOffsets
// PackingRules = Exact
struct LevelOffsets
{
    uint count;
    uint offset;
    uint parentOffset;
};

//
// Accessors for UnityEngine.Rendering.GPUPrefixSum+LevelOffsets
//
uint GetCount(LevelOffsets value)
{
    return value.count;
}
uint GetOffset(LevelOffsets value)
{
    return value.offset;
}
uint GetParentOffset(LevelOffsets value)
{
    return value.parentOffset;
}

#endif
