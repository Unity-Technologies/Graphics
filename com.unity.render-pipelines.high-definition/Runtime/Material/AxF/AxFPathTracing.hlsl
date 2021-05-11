#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

#if defined(_AXF_BRDF_TYPE_SVBRDF)
#   include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPathTracingSVBRDF.hlsl"
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
#   include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPathTracingCarPaint.hlsl"
#endif

float3 GetLightNormal(MaterialData mtlData)
{
    // If both diffuse and specular normals are quasi-indentical, return one of them, otherwise return a null vector
    return dot(GetDiffuseNormal(mtlData), GetSpecularNormal(mtlData)) > 0.99 ? GetDiffuseNormal(mtlData) : float3(0.0, 0.0, 0.0);
}

float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // Adjust the max roughness, based on the estimated diff/spec ratio
    float maxSpecRoughness = max(max(mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y), mtlData.bsdfData.roughness.z);
    float adjustedPathRoughness = (mtlResult.specPdf * maxSpecRoughness + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);

    return adjustedPathRoughness;
}

float3 ApplyAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow, float3 value)
{
    return value;
}
