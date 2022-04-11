// TODO Null implem at the moment
float4 VFXTransformPositionWorldToNonJitteredClip(float3 posWS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionWorldToPreviousClip(float3 posWS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionObjectToNonJitteredClip(float3 posOS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionObjectToPreviousClip(float3 posOS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return (float4)0.0f;
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return (float3)0.0f;
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return (float4x4)0.0f;
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return (float4x4)0.0f;
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)0.0f;
}

float3 VFXGetViewWorldPosition()
{
    return (float3)0.0f;
}

float VFXLinearEyeDepth(float depth)
{
    return 0.0f;
}

float VFXLinearEyeDepthOrthographic(float depth)
{
    return 0.0f;
}

float4 VFXApplyFog(float4 color,float4 posSS,float3 posWS)
{
    return color;
}
