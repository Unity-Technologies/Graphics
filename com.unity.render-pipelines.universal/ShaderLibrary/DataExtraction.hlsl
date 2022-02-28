#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DataExtraction.hlsl"

int UNITY_DataExtraction_Mode;
int UNITY_DataExtraction_Space;
TEXTURE2D(unity_EditorViz_DepthBuffer); SAMPLER(sampler_unity_EditorViz_DepthBuffer);

struct ExtractionInputs
{
    float3 vertexNormalWS;
    float3 pixelNormalWS;
    float3 positionWS;
    float deviceDepth;

// TODO: Implement these when DataExtraction is intended to support all material properties
//     float3 baseColor;
//     float  alpha;
//
// #ifdef _SPECULAR_SETUP
//     float3 specular;
// #else
//     float  metallic;
// #endif
//     float  smoothness;
//     float  occlusion;
//     float3 emission;
};

// These used to be in CommonMaterial.hlsl but no longer are. They are still
// needed for DataExtraction, so we have them here now.
void ConvertSpecularToMetallic(float3 diffuseColor, float3 specularColor, out float3 baseColor, out float metallic)
{
    metallic = saturate( (Max3(specularColor.r, specularColor.g, specularColor.b) - 0.1F) / 0.45F);
    baseColor = lerp(diffuseColor, specularColor, metallic);
}

void ConvertMetallicToSpecular(float3 baseColor, float metallic, out float3 diffuseColor, out float3 specularColor)
{
    diffuseColor = ComputeDiffuseColor(baseColor, metallic);
    specularColor = ComputeFresnel0(baseColor, metallic, DEFAULT_SPECULAR_VALUE);
}

float4 OutputExtraction(ExtractionInputs inputs)
{
    float3 specular, diffuse, baseColor;
    float metallic;

    // Outline mask is the most performance sensitive (rendered each frame when selection is active),
    // so it is tested first.
    // For some weird reason, using a direct return inside the branch causes a warning about
    // a potentially uninitialized variable, so use an explicitly initialized return value variable
    // to avoid it.
    float4 selectionMask = 0;
    if (UNITY_DataExtraction_Mode == RENDER_OUTLINE_MASK)
    {
        // Red channel = unique identifier, can be used to separate groups of objects from each other
        //               to get outlines between them.
        // Green channel = occluded behind depth buffer (0) or not occluded (1)
        // Blue channel  = always 1 = not cleared to zero = there's an outlined object at this pixel
        selectionMask = ComputeSelectionMask(
            0, // Object unique identifier currently unused
            ComputeNormalizedDeviceCoordinatesWithZ(inputs.positionWS, UNITY_MATRIX_VP),
            TEXTURE2D_ARGS(unity_EditorViz_DepthBuffer, sampler_unity_EditorViz_DepthBuffer));
    }

    if (UNITY_DataExtraction_Mode == RENDER_OUTLINE_MASK)
        return selectionMask;

    // #ifdef _SPECULAR_SETUP
    //     specular = inputs.specular;
    //     diffuse = inputs.baseColor;
    //     ConvertSpecularToMetallic(inputs.baseColor, inputs.specular, baseColor, metallic);
    // #else
    //     baseColor = inputs.baseColor;
    //     metallic = inputs.metallic;
    //     ConvertMetallicToSpecular(inputs.baseColor, inputs.metallic, diffuse, specular);
    // #endif

    if (UNITY_DataExtraction_Mode == RENDER_OBJECT_ID || UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return ComputePickingValue(UNITY_DataExtraction_Mode == RENDER_ENTITY_ID);
    if (UNITY_DataExtraction_Mode == RENDER_DEPTH)
        return float4(inputs.deviceDepth.xxx, 1);
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_FACE_RGB)
        return float4(PackNormalRGB(inputs.vertexNormalWS), 1.0f);
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_PIXEL_RGB)
        return float4(PackNormalRGB(inputs.pixelNormalWS), 1.0f);
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_POSITION_RGB)
        return float4(inputs.positionWS, 1.0);
    // if (UNITY_DataExtraction_Mode == RENDER_BASE_COLOR_RGBA)
    //     return float4(baseColor, inputs.alpha);
    // if (UNITY_DataExtraction_Mode == RENDER_SPECULAR_RGB)
    //     return float4(specular.xxx, 1);
    // if (UNITY_DataExtraction_Mode == RENDER_METALLIC_R)
    //     return float4(metallic.xxx, 1.0);
    // if (UNITY_DataExtraction_Mode == RENDER_EMISSION_RGB)
    //     return float4(inputs.emission, 1.0);
    // if (UNITY_DataExtraction_Mode == RENDER_SMOOTHNESS_R)
    //     return float4(inputs.smoothness.xxx, 1.0);
    // if (UNITY_DataExtraction_Mode == RENDER_OCCLUSION_R)
    //    return float4(inputs.occlusion.xxx, 1.0);
    // if (UNITY_DataExtraction_Mode == RENDER_DIFFUSE_COLOR_RGBA)
    //    return float4(diffuse, inputs.alpha);

    return 0;
}
