//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DISSOLVEOCCLUDERSDATA_CS_HLSL
#define DISSOLVEOCCLUDERSDATA_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DissolveOccludersData+DissolveOccludersCylinder
// PackingRules = Exact
struct DissolveOccludersCylinder
{
    float3 positionNDC;
    float2 radiusScaleBiasNDC;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DissolveOccludersData+DissolveOccludersCylinder
//
float3 GetPositionNDC(DissolveOccludersCylinder value)
{
    return value.positionNDC;
}
float2 GetRadiusScaleBiasNDC(DissolveOccludersCylinder value)
{
    return value.radiusScaleBiasNDC;
}

#endif
