//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDLIGHTCUSTOMFLAGS_CS_HLSL
#define HDLIGHTCUSTOMFLAGS_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDLightCustomFlags+LightCustomData
// PackingRules = Exact
struct LightCustomData
{
    uint featureFlags;
    float customRadiusScale;
    float customRadiusBias;
    float customPadding;
    float customPaddingTwo;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDLightCustomFlags+LightCustomData
//
uint GetFeatureFlags(LightCustomData value)
{
    return value.featureFlags;
}
float GetCustomRadiusScale(LightCustomData value)
{
    return value.customRadiusScale;
}
float GetCustomRadiusBias(LightCustomData value)
{
    return value.customRadiusBias;
}
float GetCustomPadding(LightCustomData value)
{
    return value.customPadding;
}
float GetCustomPaddingTwo(LightCustomData value)
{
    return value.customPaddingTwo;
}


#endif
