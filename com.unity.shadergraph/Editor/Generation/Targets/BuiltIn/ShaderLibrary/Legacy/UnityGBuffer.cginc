#ifndef UNITY_GBUFFER_INCLUDED
#define UNITY_GBUFFER_INCLUDED

//-----------------------------------------------------------------------------
// Main structure that store the data from the standard shader (i.e user input)
struct UnityStandardData
{
    half3   diffuseColor;
    half    occlusion;

    half3   specularColor;
    half    smoothness;

    float3  normalWorld;        // normal in world space
};

//-----------------------------------------------------------------------------
// This will encode UnityStandardData into GBuffer
void UnityStandardDataToGbuffer(UnityStandardData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
    outGBuffer0 = half4(data.diffuseColor, data.occlusion);

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
    outGBuffer1 = half4(data.specularColor, data.smoothness);

    // RT2: normal (rgb), --unused, very low precision-- (a)
    outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, 1.0f);
}
//-----------------------------------------------------------------------------
// This decode the Gbuffer in a UnityStandardData struct
UnityStandardData UnityStandardDataFromGbuffer(half4 inGBuffer0, half4 inGBuffer1, half4 inGBuffer2)
{
    UnityStandardData data;

    data.diffuseColor   = inGBuffer0.rgb;
    data.occlusion      = inGBuffer0.a;

    data.specularColor  = inGBuffer1.rgb;
    data.smoothness     = inGBuffer1.a;

    data.normalWorld    = normalize((float3)inGBuffer2.rgb * 2 - 1);

    return data;
}
//-----------------------------------------------------------------------------
// In some cases like for terrain, the user want to apply a specific weight to the attribute
// The function below is use for this
void UnityStandardDataApplyWeightToGbuffer(inout half4 inOutGBuffer0, inout half4 inOutGBuffer1, inout half4 inOutGBuffer2, half alpha)
{
    // With UnityStandardData current encoding, We can apply the weigth directly on the gbuffer
    inOutGBuffer0.rgb   *= alpha; // diffuseColor
    inOutGBuffer1       *= alpha; // SpecularColor and Smoothness
    inOutGBuffer2.rgb   *= alpha; // Normal
}
//-----------------------------------------------------------------------------

#endif // #ifndef UNITY_GBUFFER_INCLUDED
