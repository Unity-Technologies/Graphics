//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DISSOLVEOCCLUDERSDATA_CS_HLSL
#define DISSOLVEOCCLUDERSDATA_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DissolveOccludersData+DissolveOccludersCylinder
// PackingRules = Exact
struct DissolveOccludersCylinder
{
    float4 ellipseFromNDCScaleBias;
    float2 alphaFromEllipseScaleBias;
    float positionNDCZ;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DissolveOccludersData+DissolveOccludersCylinder
//
float4 GetEllipseFromNDCScaleBias(DissolveOccludersCylinder value)
{
    return value.ellipseFromNDCScaleBias;
}
float2 GetAlphaFromEllipseScaleBias(DissolveOccludersCylinder value)
{
    return value.alphaFromEllipseScaleBias;
}

#endif
