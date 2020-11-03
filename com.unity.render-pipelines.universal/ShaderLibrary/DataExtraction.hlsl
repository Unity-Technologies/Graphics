
int UNITY_DataExtraction_Mode;
int UNITY_DataExtraction_Space;

#define RENDER_OBJECT_ID               1
#define RENDER_DEPTH                   2
#define RENDER_WORLD_NORMALS_FACE_RGB  3
#define RENDER_WORLD_POSITION_RGB      4
#define RENDER_ENTITY_ID               5
#define RENDER_BASE_COLOR_RGBA         6
#define RENDER_SPECULAR_RGB            7
#define RENDER_METALLIC_R              8
#define RENDER_EMISSION_RGB            9
#define RENDER_WORLD_NORMALS_PIXEL_RGB 10
#define RENDER_SMOOTHNESS_R            11
#define RENDER_OCCLUSION_R             12
#define RENDER_DIFFUSE_COLOR_RGBA      13

struct ExtractionInputs
{
    float3 vertexNormalWS;
    float3 pixelNormalWS;
    float3 positionWS;

    float3 baseColor;
    float  alpha;

#ifdef _SPECULAR_SETUP
    float3 specular;
#else
    float  metallic;
#endif
    float  smoothness;
    float  occlusion;
    float3 emission;
};

float4 PackEntityIdToRGBA8888(uint entityId)
{
    uint b0 = (entityId >>  0) & 0xff;
    uint b1 = (entityId >>  8) & 0xff;
    uint b2 = (entityId >> 16) & 0xff;
    uint b3 = (entityId >> 24) & 0xff;
    float f0 = (float)b0 / 255.0f;
    float f1 = (float)b1 / 255.0f;
    float f2 = (float)b2 / 255.0f;
    float f3 = (float)b3 / 255.0f;
    return float4(f0, f1, f2, f3);
}

float4 OutputExtraction(ExtractionInputs inputs)
{
    float3 specular, diffuse, baseColor;
    float metallic;

    #ifdef _SPECULAR_SETUP
        specular = inputs.specular;
        diffuse = inputs.baseColor;
        ConvertSpecularToMetallic(inputs.baseColor, inputs.specular, baseColor, metallic);
    #else
        baseColor = inputs.baseColor;
        metallic = inputs.metallic;
        ConvertMetallicToSpecular(inputs.baseColor, inputs.metallic, diffuse, specular);
    #endif

    if (UNITY_DataExtraction_Mode == RENDER_OBJECT_ID)
        return float4(asint(unity_LODFade.z), 0, 0, 1);
    //@TODO
    if (UNITY_DataExtraction_Mode == RENDER_DEPTH)
        return 0;
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_FACE_RGB)
        return float4(PackNormalRGB(inputs.vertexNormalWS), 1.0f);
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_POSITION_RGB)
        return float4(inputs.positionWS, 1.0);
    //@TODO
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    if (UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return PackEntityIdToRGBA8888(unity_EntityId.x);
#else
    if (UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return 0;
#endif
    if (UNITY_DataExtraction_Mode == RENDER_BASE_COLOR_RGBA)
        return float4(baseColor, inputs.alpha);
    if (UNITY_DataExtraction_Mode == RENDER_SPECULAR_RGB)
        return float4(specular.xxx, 1);
    if (UNITY_DataExtraction_Mode == RENDER_METALLIC_R)
        return float4(metallic.xxx, 1.0);
    if (UNITY_DataExtraction_Mode == RENDER_EMISSION_RGB)
        return float4(inputs.emission, 1.0);
    if (UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_PIXEL_RGB)
        return float4(PackNormalRGB(inputs.pixelNormalWS), 1.0f);
    if (UNITY_DataExtraction_Mode == RENDER_SMOOTHNESS_R)
        return float4(inputs.smoothness.xxx, 1.0);
    if (UNITY_DataExtraction_Mode == RENDER_OCCLUSION_R)
       return float4(inputs.occlusion.xxx, 1.0);
    if (UNITY_DataExtraction_Mode == RENDER_DIFFUSE_COLOR_RGBA)
       return float4(diffuse, inputs.alpha);

    return 0;
}




