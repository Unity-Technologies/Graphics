//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Unlit.cs.hlsl"

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData data)
{
    BSDFData output;
    output.color = data.color;

    return output;
}

//-----------------------------------------------------------------------------
// No light evaluation, this is unlit
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEW_UNLIT_SURFACEDATA_COLOR:
        result = surfaceData.color; needLinearToSRGB = true;
        break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEW_UNLIT_SURFACEDATA_COLOR:
        result = bsdfData.color; needLinearToSRGB = true;
        break;
    }
}

LighTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LighTransportData lightTransportData;

    lightTransportData.diffuseColor = float3(0.0, 0.0, 0.0);
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}
