//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.cs.hlsl"
#if defined(WRITE_NORMAL_BUFFER)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#endif
#if defined(_ENABLE_SHADOW_MATTE)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#endif
//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData data)
{
    BSDFData output;
    output.color = data.color;

    return output;
}

#if defined(WRITE_NORMAL_BUFFER)
NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    // Note: When we are in the prepass (depth forward only) and we need to export the normal. This also requires a roughness value, we export a fake one (0.0)
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = 0.0;
    return normalData;
}
#endif

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// No light evaluation, this is unlit
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    lightTransportData.diffuseColor = float3(0.0, 0.0, 0.0);
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}
