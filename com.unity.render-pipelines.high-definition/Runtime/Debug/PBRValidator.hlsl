#ifndef UNITY_DEBUG_EDITOR_VIZ_INCLUDED
#define UNITY_DEBUG_EDITOR_VIZ_INCLUDED

// Define bounds value in linear RGB for fresnel0 values
// Note: "static const" qualifier is mandatory, "const" alone doesn't work
static const float dieletricMin = 0.02;
static const float dieletricMax = 0.07;
static const float gemsMin      = 0.07;
static const float gemsMax      = 0.22;
static const float conductorMin = 0.45;
static const float conductorMax = 1.00;
static const float albedoMin    = 0.012;
static const float albedoMax    = 0.9;

// Diffuse Color validation
float4 pbrDiffuseColorValidate(float3 diffuseColor, float3 specularColor, bool isMetal, bool metallicWorkflow)
{
    float3 unTouched = Luminance(diffuseColor).xxx; // if no errors, leave color as it was in render

    if (!metallicWorkflow)
    {
        isMetal = dot(specularColor, float3(0.3333, 0.3333, 0.3333)) >= conductorMin;
    }

    // When checking full range we do not take the luminance but the mean because often in game blue color are highlight as too low whereas this is what we are looking for.
    float value = dot(diffuseColor, float3(0.3333, 0.3333, 0.3333));

    // Check if we are pure metal with black albedo
    if (_DebugLightingMaterialValidatePureMetalColor.x > 0.0 && isMetal && value != 0.0)
    {
    return float4(_DebugLightingMaterialValidatePureMetalColor.yzw, 0);
    }

    // If we have a metallic object, don't complain about low albedo
    if (!isMetal && value < albedoMin)
    {
       return _DebugLightingMaterialValidateLowColor;
    }
    else if (value > albedoMax)
    {
        return _DebugLightingMaterialValidateHighColor;
    }
    else
    {
       return float4(unTouched, 0);
    }

    return float4(unTouched, 0);
}

// Specular Color validation
float4 pbrSpecularColorValidate(float3 diffuseColor, float3 specularColor, bool isMetal, bool metallicWorkflow)
{

    float value = dot(specularColor, float3(0.3333,0.3333,0.3333));

    if (!metallicWorkflow)
    {
        isMetal = value >= conductorMin;
    }

    float4 outColor = float4(Luminance(diffuseColor.xyz).xxx, 1.0f);

    if (value < conductorMin && isMetal)
    {
         outColor = _DebugLightingMaterialValidateLowColor;
    }
    else if (value > conductorMax)
    {
        outColor = _DebugLightingMaterialValidateHighColor;
    }
    else if (isMetal)
    {
         // If we are here we supposed the users want to have a metal, so check if we have a pure metal (black albedo) or not
        // if it is not a pure metal, highlight it
       if (_DebugLightingMaterialValidatePureMetalColor.x > 0.0)
       {
            outColor = dot(diffuseColor.xyz, float3(1,1,1)) == 0 ? outColor : float4(_DebugLightingMaterialValidatePureMetalColor.yzw, 0);
       }
    }

    return outColor;
}

#endif
