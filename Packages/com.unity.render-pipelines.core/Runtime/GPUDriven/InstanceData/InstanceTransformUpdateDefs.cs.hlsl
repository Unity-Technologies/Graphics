//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef INSTANCETRANSFORMUPDATEDEFS_CS_HLSL
#define INSTANCETRANSFORMUPDATEDEFS_CS_HLSL
// Generated from UnityEngine.Rendering.SHUpdatePacket
// PackingRules = Exact
struct SHUpdatePacket
{
    float shr0;
    float shr1;
    float shr2;
    float shr3;
    float shr4;
    float shr5;
    float shr6;
    float shr7;
    float shr8;
    float shg0;
    float shg1;
    float shg2;
    float shg3;
    float shg4;
    float shg5;
    float shg6;
    float shg7;
    float shg8;
    float shb0;
    float shb1;
    float shb2;
    float shb3;
    float shb4;
    float shb5;
    float shb6;
    float shb7;
    float shb8;
};

// Generated from UnityEngine.Rendering.TransformUpdatePacket
// PackingRules = Exact
struct TransformUpdatePacket
{
    float4 localToWorld0;
    float4 localToWorld1;
    float4 localToWorld2;
};

//
// Accessors for UnityEngine.Rendering.SHUpdatePacket
//
float GetShr0(SHUpdatePacket value)
{
    return value.shr0;
}
float GetShr1(SHUpdatePacket value)
{
    return value.shr1;
}
float GetShr2(SHUpdatePacket value)
{
    return value.shr2;
}
float GetShr3(SHUpdatePacket value)
{
    return value.shr3;
}
float GetShr4(SHUpdatePacket value)
{
    return value.shr4;
}
float GetShr5(SHUpdatePacket value)
{
    return value.shr5;
}
float GetShr6(SHUpdatePacket value)
{
    return value.shr6;
}
float GetShr7(SHUpdatePacket value)
{
    return value.shr7;
}
float GetShr8(SHUpdatePacket value)
{
    return value.shr8;
}
float GetShg0(SHUpdatePacket value)
{
    return value.shg0;
}
float GetShg1(SHUpdatePacket value)
{
    return value.shg1;
}
float GetShg2(SHUpdatePacket value)
{
    return value.shg2;
}
float GetShg3(SHUpdatePacket value)
{
    return value.shg3;
}
float GetShg4(SHUpdatePacket value)
{
    return value.shg4;
}
float GetShg5(SHUpdatePacket value)
{
    return value.shg5;
}
float GetShg6(SHUpdatePacket value)
{
    return value.shg6;
}
float GetShg7(SHUpdatePacket value)
{
    return value.shg7;
}
float GetShg8(SHUpdatePacket value)
{
    return value.shg8;
}
float GetShb0(SHUpdatePacket value)
{
    return value.shb0;
}
float GetShb1(SHUpdatePacket value)
{
    return value.shb1;
}
float GetShb2(SHUpdatePacket value)
{
    return value.shb2;
}
float GetShb3(SHUpdatePacket value)
{
    return value.shb3;
}
float GetShb4(SHUpdatePacket value)
{
    return value.shb4;
}
float GetShb5(SHUpdatePacket value)
{
    return value.shb5;
}
float GetShb6(SHUpdatePacket value)
{
    return value.shb6;
}
float GetShb7(SHUpdatePacket value)
{
    return value.shb7;
}
float GetShb8(SHUpdatePacket value)
{
    return value.shb8;
}
//
// Accessors for UnityEngine.Rendering.TransformUpdatePacket
//
float4 GetLocalToWorld0(TransformUpdatePacket value)
{
    return value.localToWorld0;
}
float4 GetLocalToWorld1(TransformUpdatePacket value)
{
    return value.localToWorld1;
}
float4 GetLocalToWorld2(TransformUpdatePacket value)
{
    return value.localToWorld2;
}

#endif
