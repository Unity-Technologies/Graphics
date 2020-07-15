
int UNITY_DataExtraction_Mode;
int UNITY_DataExtraction_Space;

#define RENDER_OBJECT_ID 1
#define RENDER_DEPTH 2
#define RENDER_WORLD_NORMALS_FACE_RGB 3
#define RENDER_WORLD_POSITION_RGB 4
#define RENDER_ENTITY_ID 5
#define RENDER_BASE_COLOR_RGB 6
#define RENDER_SPECULAR_RGB 7
#define RENDER_METALLIC_RGB 8
#define RENDER_EMISSION_RGB 9
#define RENDER_WORLD_NORMALS_PIXEL_RGB 10
#define RENDER_SMOOTHNESS_RGB 11
#define RENDER_OCCLUSION_RGB 12
#define RENDER_DIFFUSE_COLOR_RGB 13


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
