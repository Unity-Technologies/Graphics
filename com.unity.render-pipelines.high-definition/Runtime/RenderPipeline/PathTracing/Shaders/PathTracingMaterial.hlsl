#define MAX_BSDF_COUNT 4
#define BSDF_WEIGHT_EPSILON 0.001

struct MaterialData
{
    BSDFData bsdfData;
    float    bsdfWeight[MAX_BSDF_COUNT];
    uint     bsdfCount;
    float3   V;
};

struct MaterialResult
{
    float3 diffValue;
    float  diffPdf;
    float3 specValue;
    float  specPdf;
};

void Init(inout MaterialResult result)
{
    result.diffValue = 0.0;
    result.diffPdf = 0.0;
    result.specValue = 0.0;
    result.specPdf = 0.0;
}

void InitDiffuse(inout MaterialResult result)
{
    result.diffValue = 0.0;
    result.diffPdf = 0.0;
}

void InitSpecular(inout MaterialResult result)
{
    result.specValue = 0.0;
    result.specPdf = 0.0;
}

bool IsBlack(MaterialData mtlData)
{
    float wSum = 0.0;
    for (uint i = 0; i < mtlData.bsdfCount; i++)
        wSum += mtlData.bsdfWeight[i];
    return wSum < BSDF_WEIGHT_EPSILON;
}

bool IsAbove(float3 normalWS, float3 dirWS)
{
    return dot(normalWS, dirWS) >= 0.0;
}

bool IsAbove(MaterialData mtlData, float3 dirWS)
{
    return IsAbove(mtlData.bsdfData.geomNormalWS, dirWS);
}

bool IsAbove(MaterialData mtlData)
{
    return IsAbove(mtlData.bsdfData.geomNormalWS, mtlData.V);
}

bool IsBelow(float3 normalWS, float3 dirWS)
{
    return !IsAbove(normalWS, dirWS);
}

bool IsBelow(MaterialData mtlData, float3 dirWS)
{
    return !IsAbove(mtlData, dirWS);
}

bool IsBelow(MaterialData mtlData)
{
    return !IsAbove(mtlData);
}

float3x3 GetTangentFrame(MaterialData mtlData)
{
    return mtlData.bsdfData.anisotropy != 0.0 ?
        float3x3(mtlData.bsdfData.tangentWS, mtlData.bsdfData.bitangentWS, mtlData.bsdfData.normalWS) :
        GetLocalFrame(mtlData.bsdfData.normalWS);
}
