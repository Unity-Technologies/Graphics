#if SHADERPASS != SHADERPASS_LIGHT_TRANSPORT
#error SHADERPASS_is_not_correctly_define
#endif

#include "Color.hlsl"

// TODO: This is the max value allowed for emissive (bad name - but keep for now to retrieve it) (It is 8^2.2 (gamma) and 8 is the limit of punctual light slider...), comme from UnityCg.cginc. Fix it!
// Ask Jesper if this can be change for HDRenderPipeline
#define EMISSIVE_RGBM_SCALE 97.0

float4 Frag(PackedVaryings packedInput) : SV_Target
{
    FragInputs input = UnpackVaryings(packedInput);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
    // No position and depth in case of light transport
    float3 V = float3(0, 0, 1); // No vector view in case of light transport

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    LighTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two time. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.

    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    // TODO: No if / else in original code from Unity, why ? keep like original code but should be either diffuse or emissive
    if (unity_MetaFragmentControl.x)
    {
        // Apply diffuseColor Boost from LightmapSettings.
        // put abs here to silent a warning, no cost, no impact as color is assume to be positive.
        res.rgb = Clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }
    
    if (unity_MetaFragmentControl.y)
    {
        // TODO: THIS LIMIT MUST BE REMOVE, IT IS NOT HDR, change when RGB9e5 is here.
        // Do we assume here that emission is [0..1] ?
        res = PackRGBM(lightTransportData.emissiveColor, EMISSIVE_RGBM_SCALE);
    }

    return res;
}
