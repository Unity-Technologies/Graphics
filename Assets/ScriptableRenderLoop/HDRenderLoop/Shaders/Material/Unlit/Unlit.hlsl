#ifndef UNITY_UNLIT_INCLUDED
#define UNITY_UNLIT_INCLUDED

struct SurfaceData
{
    float3 color;
};

struct BSDFData
{
    float3 color;
};

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


#endif // UNITY_UNLIT_INCLUDED
