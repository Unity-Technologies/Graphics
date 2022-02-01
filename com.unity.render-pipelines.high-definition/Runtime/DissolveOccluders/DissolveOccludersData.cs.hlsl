//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef DISSOLVEOCCLUDERSDATA_CS_HLSL
#define DISSOLVEOCCLUDERSDATA_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.DissolveOccludersData+DissolveOccludersCylinder
// PackingRules = Exact
struct DissolveOccludersCylinder
{
    float4 ellipseFromNDCScaleBias;
    float2 alphaFromEllipseScaleBias;
    float positionNDCZ;
    float positionWSY;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.DissolveOccludersData+DissolveOccludersCylinder
//
float4 GetEllipseFromNDCScaleBias(DissolveOccludersCylinder value)
{
    return value.ellipseFromNDCScaleBias;
}
float2 GetAlphaFromEllipseScaleBias(DissolveOccludersCylinder value)
{
    return value.alphaFromEllipseScaleBias;
}
float GetPositionNDCZ(DissolveOccludersCylinder value)
{
    return value.positionNDCZ;
}
float GetPositionWSY(DissolveOccludersCylinder value)
{
    return value.positionWSY;
}

#endif
