//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef BRGTRANSFORMUPDATERDEFS_CS_HLSL
#define BRGTRANSFORMUPDATERDEFS_CS_HLSL
// Generated from UnityEngine.Rendering.BRGGpuTransformUpdate
// PackingRules = Exact
struct BRGGpuTransformUpdate
{
    float4 localToWorld0; // x: x y: y z: z w: w
    float4 localToWorld1; // x: x y: y z: z w: w
    float4 localToWorld2; // x: x y: y z: z w: w
    float4 worldToLocal0; // x: x y: y z: z w: w
    float4 worldToLocal1; // x: x y: y z: z w: w
    float4 worldToLocal2; // x: x y: y z: z w: w
};

//
// Accessors for UnityEngine.Rendering.BRGGpuTransformUpdate
//
float4 GetLocalToWorld0(BRGGpuTransformUpdate value)
{
    return value.localToWorld0;
}
float4 GetLocalToWorld1(BRGGpuTransformUpdate value)
{
    return value.localToWorld1;
}
float4 GetLocalToWorld2(BRGGpuTransformUpdate value)
{
    return value.localToWorld2;
}
float4 GetWorldToLocal0(BRGGpuTransformUpdate value)
{
    return value.worldToLocal0;
}
float4 GetWorldToLocal1(BRGGpuTransformUpdate value)
{
    return value.worldToLocal1;
}
float4 GetWorldToLocal2(BRGGpuTransformUpdate value)
{
    return value.worldToLocal2;
}

#endif
