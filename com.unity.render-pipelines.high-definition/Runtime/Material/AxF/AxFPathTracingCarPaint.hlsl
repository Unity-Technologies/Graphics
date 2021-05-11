#ifndef UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED
#define UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, MaterialData mtlData, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = float3(1.0, 0.0, 1.0);
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.V = -WorldRayDirection();
    mtlData.Nv = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.normalWS);
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(mtlData))
    {
        mtlData.bsdfWeight[0] = Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;
    }

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2] + mtlData.bsdfWeight[3];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

    return true;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        if (!BRDF::SampleDiffuse(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
            return false;
    }

    return result.diffPdf + result.specPdf > 0.0;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        BRDF::EvaluateDiffuse(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
    }
}

#endif // UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED
