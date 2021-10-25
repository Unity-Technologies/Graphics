#ifndef UNITY_PATH_TRACING_MATERIAL_INCLUDED
#define UNITY_PATH_TRACING_MATERIAL_INCLUDED

#define BSDF_WEIGHT_EPSILON 0.00001

struct MaterialData
{
    // BSDFs (4 max)
    BSDFData bsdfData;
    float4   bsdfWeight;

    // Subsurface scattering
    bool     isSubsurface;
    float    subsurfaceWeightFactor;

    // View vector, and altered shading normal
    // (to be consistent with the view vector and geometric normal)
    float3   V;
    float3   Nv;
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

float3 GetDiffuseNormal(MaterialData mtlData)
{
    return mtlData.bsdfData.normalWS;
}

float3 GetSpecularNormal(MaterialData mtlData)
{
    return mtlData.Nv;
}

float3 ComputeConsistentShadingNormal(float3 Wi, float3 G, float3 N)
{
    // Check in which hemisphere does the incoming view vector fall
    float GdotWi = dot(G, Wi);
    float Hi = sign(GdotWi);

    // First project N back towards Wi if it's on the other side of the view plane
    float NdotWi = Hi * dot(N, Wi);
    float3 Ni = N - Hi * min(0.0, NdotWi) * Wi;

    // Then check in which hemisphere does the reflected vector fall
    float3 Wo = reflect(-Wi, Ni);
    float GdotWo = dot(G, Wo);
    float Ho = sign(GdotWo);

    // Bring reflection direction back to the right hemisphere (slightly offset from the horizon, for more robustness)
    Wo = normalize(Wo - (GdotWo + Ho * 0.001) * G);

    // Compute a new, consistent shading normal accordingly
    return Hi != Ho ? Hi * normalize(Wi + Wo) : N;
}

#endif // UNITY_PATH_TRACING_MATERIAL_INCLUDED
