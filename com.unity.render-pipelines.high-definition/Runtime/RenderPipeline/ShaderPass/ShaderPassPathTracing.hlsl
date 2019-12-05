// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"

// Path tracing includes
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitPathTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/SubSurface.hlsl"
#endif

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

#ifdef HAS_LIGHTLOOP
bool SampleGGX(BSDFData bsdfData,
                float3 viewWS,
               float2 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float NdotL, NdotH, VdotH;
    float3x3 localToWorld = GetLocalFrame(bsdfData.normalWS);
    SampleGGXDir(inputSample, viewWS, localToWorld, bsdfData.roughnessT, outgoingDir, NdotL, NdotH, VdotH);

    if (NdotL < 0.001 || !IsAbove(bsdfData.normalWS, outgoingDir))
        return false;

    float D = D_GGX(NdotH, bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    if (pdf < 0.001)
        return false;

    float NdotV = dot(bsdfData.normalWS, viewWS);
    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
    float Vg = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);

    value = F * D * Vg * NdotL;

    return true;
}
#endif

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Compute the viewv ector
    float3 viewWS = -WorldRayDirection();

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - rayIntersection.remainingDepth;
    const float3 positionWS = GetAbsolutePositionWS(fragInput.positionRWS);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // If we are in the case of a shadow ray or a volumetric ray, we do not have anything left to do
    if (rayIntersection.rayType == SHADOW_RAY || rayIntersection.rayType == VOLUMETRIC_RAY)
    {
        rayIntersection.outPosition = positionWS;
        rayIntersection.normal = fragInput.tangentToWorld[2];
        return;
    }

#ifdef HAS_LIGHTLOOP

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    if (rayIntersection.remainingDepth > 1)
    {
        // Generate the new sample (following values of the sequence)
        float4 inputSample = 0.0;
        inputSample.x = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth);
        inputSample.y = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 1);
        inputSample.z = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 2);
        inputSample.w = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 3);

        // Variables that hold information about the exit point of this intersection
        float3 exitPosition;
        float3 exitNormal;
        float3 exitDirection;
        float3 throughput;
        float brdfDiffusePDF;
        float brdfSpecPDF;

        // Compute fresnel for picking a lobe
        float  NdotV = dot(bsdfData.normalWS, viewWS);
        float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
        float lumF = Luminance(F);

        bool consistentNormal = (NdotV > 0.001);
        float diffuseWeight = Luminance(bsdfData.diffuseColor);
        float specularWeight = consistentNormal ? lerp(lumF, 0.5, bsdfData.roughnessT): 0.0;

        specularWeight /= diffuseWeight + specularWeight;    
        diffuseWeight = 1.0 - specularWeight;

        if (inputSample.z < specularWeight)
        {
            exitPosition = positionWS;
            exitNormal = bsdfData.normalWS;
            SampleGGX(bsdfData, viewWS, inputSample.xy, exitDirection, throughput, brdfSpecPDF);
            brdfDiffusePDF = dot(bsdfData.normalWS, exitDirection) * INV_PI;
            brdfSpecPDF *= specularWeight;
            throughput += bsdfData.diffuseColor * brdfDiffusePDF;
        }
        else
        {
            // Fill the exit information based on the surface type
            float isVolumetric = IsVolumetric(bsdfData) && IsAbove(bsdfData.normalWS, viewWS);
            if (isVolumetric)
            {
                throughput = 1.0;
                ScatteringResult scatteringResult = ScatteringWalk(bsdfData, rayIntersection, positionWS, viewWS, throughput);
                exitPosition = scatteringResult.outputPosition;
                exitNormal = scatteringResult.outputNormal;
                exitDirection = scatteringResult.outputDirection;
                brdfDiffusePDF = scatteringResult.outputPDF;
                throughput *= scatteringResult.outputPDF;
                throughput *= diffuseWeight;
                brdfSpecPDF = 0.0;

            }
            else
            {
                exitPosition = positionWS;
                exitNormal = bsdfData.normalWS;
                exitDirection = SampleHemisphereCosine(inputSample.x, inputSample.y, bsdfData.normalWS);
                brdfDiffusePDF = dot(bsdfData.normalWS, exitDirection) * INV_PI;
                throughput = bsdfData.diffuseColor * brdfDiffusePDF;

                brdfDiffusePDF *= diffuseWeight;
                float3 H = normalize((exitDirection + viewWS) * 0.5f);
                float NdotH = dot(H, bsdfData.normalWS);
                float VdotH = dot(H, viewWS);
                float D = D_GGX(NdotH, bsdfData.roughnessT);
                brdfSpecPDF = D * NdotH / (4.0 * VdotH);

                float NdotL = dot(bsdfData.normalWS, exitDirection);
                float Vg = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);
                float3 brdfGGX = F * D * Vg * NdotL;
                throughput += brdfGGX;
            }
        }

        // make sure its empty before adding to it
        rayIntersection.color = 0.0;

        // Create a ray descriptor for the next ray
        RayDesc rayDescriptor;
        rayDescriptor.Origin = exitPosition + exitNormal * _RaytracingRayBias;
        rayDescriptor.Direction = exitDirection;
        rayDescriptor.TMin = 0.0;
        rayDescriptor.TMax = FLT_INF;

        RayIntersection nextRayIntersection;
        nextRayIntersection.color = float3(1.0, 1.0, 1.0);
        nextRayIntersection.incidentDirection = exitDirection;
        nextRayIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
        nextRayIntersection.pixelCoord = rayIntersection.pixelCoord;
        nextRayIntersection.rayType = MAIN_RAY;
        nextRayIntersection.rayCount = rayIntersection.rayCount;

        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE
                                                , RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

        float combinedPDF = brdfDiffusePDF + brdfSpecPDF;
        throughput = SafeDivide(throughput, combinedPDF);
        throughput = AnyIsNaN(throughput) ? float3(0.0, 0.0, 0.0) : throughput;

        rayIntersection.color += nextRayIntersection.color * throughput;

    }
    else
    {
        rayIntersection.color = 0.0;
    }
#else // HAS_LIGHTLOOP
    rayIntersection.color = builtinData.emissiveColor * GetCurrentExposureMultiplier();
#endif
}

/*
// Handles fully transparent objects (not called if RAY_FLAG_FORCE_OPAQUE is set)
[shader("anyhit")]
void AnyHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Compute the distance of the ray
    rayIntersection.t = RayTCurrent();

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = 0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, -WorldRayDirection(), posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // If this fella should be culled, then we cull it
    if (!isVisible)
        IgnoreHit();

    // If the depth information is marked as invalid, we are shooting a transmission ray
    if (rayIntersection.remainingDepth > _RaytracingMaxRecursion)
        AcceptHitAndEndSearch();
}
*/
