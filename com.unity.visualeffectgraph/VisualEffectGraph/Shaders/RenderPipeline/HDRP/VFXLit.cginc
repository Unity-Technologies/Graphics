#ifdef DEBUG_DISPLAY
#include "HDRP/Debug/DebugDisplay.hlsl"
#endif
#include "HDRP/Material/Material.hlsl"
//#include "HDRP/Material/BuiltIn/BuiltInData.cs.hlsl"

float3 VFXSampleLightProbes(float3 normalWS)
{
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return SampleSH9(SHCoefficients, normalWS);
}

float3 VFXGetAmbient(float3 normalWS)
{
    // TODO Handle LP proxy volumes
    return VFXSampleLightProbes(normalWS);
}
