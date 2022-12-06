#ifndef UNITY_PATH_TRACING_BSDF_INCLUDED
#define UNITY_PATH_TRACING_BSDF_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/SubSurface.hlsl"

#define DELTA_PDF 1000000.0
#define MIN_GGX_ROUGHNESS 0.00001
#define MAX_GGX_ROUGHNESS 0.99999

float3x3 GetTangentFrame(float3 tangent, float3 bitangent, float3 normal, bool anisotropic)
{
    // If we have anisotropy, we want our local frame to follow tangential directions, otherwise any orientation will do
    return anisotropic ? float3x3(tangent, bitangent, normal) : GetLocalFrame(normal);
}

float Lambda_AnisoGGX(float roughnessX,
                      float roughnessY,
                      float3 V)
{
    return 0.5 * (sqrt(1.0 + (Sq(roughnessX * V.x) + Sq(roughnessY * V.y)) / Sq(V.z)) - 1.0);
}

float G_AnisoGGX(float roughnessX,
                 float roughnessY,
                 float3 V)
{
    return rcp(1.0 + Lambda_AnisoGGX(roughnessX, roughnessY, V));
}

float D_AnisoGGX(float roughnessX,
                 float roughnessY,
                 float3 H)
{
    return rcp(PI * roughnessX * roughnessY * Sq(Sq(H.x / roughnessX) + Sq(H.y / roughnessY) + Sq(H.z)));
}

namespace BRDF
{

float GetGGXMultipleScatteringEnergy(float roughness, float sqrtNdotV)
{
    float2 coordLUT = Remap01ToHalfTexelCoord(float2(sqrtNdotV, roughness), FGDTEXTURE_RESOLUTION);
    float E = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_GGXDisneyDiffuse, s_linear_clamp_sampler, coordLUT, 0).y;
    return (1.0 - E) / E;
}

bool SampleAnisoGGX(MaterialData mtlData,
                    float3 normal,
                    float roughnessX,
                    float roughnessY,
                    float3 fresnel0,
                    float3 inputSample,
                out float3 outgoingDir,
                out float3 value,
                out float pdf,
                out float3 fresnel)
{
    roughnessX = clamp(roughnessX, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);
    roughnessY = clamp(roughnessY, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

    float VdotH;
    float3 localV, localH;
    float3x3 localToWorld = GetTangentFrame(mtlData.bsdfData.tangentWS,
                                            mtlData.bsdfData.bitangentWS,
                                            normal, roughnessX != roughnessY);
    SampleAnisoGGXVisibleNormal(inputSample.xy, mtlData.V, localToWorld, roughnessX, roughnessY, localV, localH, VdotH);

    // Compute the reflection direction
    float3 localL = 2.0 * VdotH * localH - localV;
    outgoingDir = mul(localL, localToWorld);

    if (localL.z < 0.001 || !IsAbove(mtlData, outgoingDir))
        return false;

    float pdfNoGV = D_AnisoGGX(roughnessX, roughnessY, localH) / (4.0 * localV.z);
    float lambdaVPlusOne = Lambda_AnisoGGX(roughnessX, roughnessY, localV) + 1.0;
    pdf = pdfNoGV / lambdaVPlusOne;

    if (pdf < 0.001)
        return false;

    float lambdaL = Lambda_AnisoGGX(roughnessX, roughnessY, localL);
    fresnel = F_Schlick(fresnel0, VdotH);
    value = fresnel * pdfNoGV / (lambdaVPlusOne + lambdaL);

    return true;
}

bool SampleAnisoGGX(MaterialData mtlData,
                    float3 normal,
                    float roughnessX,
                    float roughnessY,
                    float3 fresnel0,
                    float3 inputSample,
                out float3 outgoingDir,
                out float3 value,
                out float pdf)
{
    float3 dummyFresnel;
    return SampleAnisoGGX(mtlData, normal, roughnessX, roughnessY, fresnel0, inputSample, outgoingDir, value, pdf, dummyFresnel);
}

void EvaluateAnisoGGX(MaterialData mtlData,
                      float3 normal,
                      float roughnessX,
                      float roughnessY,
                      float3 fresnel0,
                      float3 outgoingDir,
                  out float3 value,
                  out float pdf,
                  out float3 fresnel)
{
    float NdotV = dot(normal, mtlData.V);
    if (NdotV < 0.001)
    {
        value = 0.0;
        pdf = 0.0;
        fresnel = 0.0;
        return;
    }

    roughnessX = clamp(roughnessX, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);
    roughnessY = clamp(roughnessY, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

    float3x3 worldToLocal = transpose(GetTangentFrame(mtlData.bsdfData.tangentWS,
                                                      mtlData.bsdfData.bitangentWS,
                                                      normal, roughnessX != roughnessY));
    float3 localV = mul(mtlData.V, worldToLocal);
    float3 localL = mul(outgoingDir, worldToLocal);
    float3 localH = normalize(localV + localL);
    float VdotH = dot(localV, localH);

    float pdfNoGV = D_AnisoGGX(roughnessX, roughnessY, localH) / (4.0 * localV.z);
    float lambdaVPlusOne = Lambda_AnisoGGX(roughnessX, roughnessY, localV) + 1.0;
    float lambdaL = Lambda_AnisoGGX(roughnessX, roughnessY, localL);

    fresnel = F_Schlick(fresnel0, VdotH);
    value = fresnel * pdfNoGV / (lambdaVPlusOne + lambdaL);
    pdf = pdfNoGV / lambdaVPlusOne;
}

void EvaluateAnisoGGX(MaterialData mtlData,
                      float3 normal,
                      float roughnessX,
                      float roughnessY,
                      float3 fresnel0,
                      float3 outgoingDir,
                  out float3 value,
                  out float pdf)
{
    float3 dummyFresnel;
    EvaluateAnisoGGX(mtlData, normal, roughnessX, roughnessY, fresnel0, outgoingDir, value, pdf, dummyFresnel);
}

bool SampleGGX(MaterialData mtlData,
               float3 normal,
               float roughness,
               float3 fresnel0,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf,
           out float3 fresnel)
{
    return SampleAnisoGGX(mtlData, normal, roughness, roughness, fresnel0, inputSample, outgoingDir, value, pdf, fresnel);
}

bool SampleGGX(MaterialData mtlData,
               float3 normal,
               float roughness,
               float3 fresnel0,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float3 dummyFresnel;
    return SampleGGX(mtlData, normal, roughness, fresnel0, inputSample, outgoingDir, value, pdf, dummyFresnel);
}

void EvaluateGGX(MaterialData mtlData,
                 float3 normal,
                 float roughness,
                 float3 fresnel0,
                 float3 outgoingDir,
             out float3 value,
             out float pdf,
             out float3 fresnel)
{
    return EvaluateAnisoGGX(mtlData, normal, roughness, roughness, fresnel0, outgoingDir, value, pdf, fresnel);
}

void EvaluateGGX(MaterialData mtlData,
                float3 normal,
                float roughness,
                float3 fresnel0,
                float3 outgoingDir,
            out float3 value,
            out float pdf)
{
    float3 dummyFresnel;
    EvaluateGGX(mtlData, normal, roughness, fresnel0, outgoingDir, value, pdf, dummyFresnel);
}

bool SampleDelta(MaterialData mtlData,
                 float3 normal,
                 float ior,
             out float3 outgoingDir,
             out float3 value,
             out float pdf)
{
    if (IsAbove(mtlData))
    {
        outgoingDir = reflect(-mtlData.V, normal);
        float NdotV = dot(normal, mtlData.V);
        value = F_Schlick(mtlData.bsdfData.fresnel0, NdotV);
    }
    else // Below
    {
        outgoingDir = -reflect(mtlData.V, normal);
        float NdotV = -dot(normal, mtlData.V);
        value = F_FresnelDielectric(1.0 / ior, NdotV);
    }

    value *= DELTA_PDF;
    pdf = DELTA_PDF;

    return any(outgoingDir);
}

bool SampleLambert(MaterialData mtlData,
                   float3 normal,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, normal);

    if (!IsAbove(mtlData, outgoingDir))
        return false;

    pdf = dot(normal, outgoingDir) * INV_PI;

    if (pdf < 0.001)
        return false;

    value = mtlData.bsdfData.diffuseColor * pdf;

    return true;
}

void EvaluateLambert(MaterialData mtlData,
                     float3 normal,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
    pdf = saturate(dot(normal, outgoingDir)) * INV_PI;
    value = mtlData.bsdfData.diffuseColor * pdf;
}

#ifndef USE_DIFFUSE_LAMBERT_BRDF

bool SampleBurley(MaterialData mtlData,
                  float3 normal,
                  float3 inputSample,
              out float3 outgoingDir,
              out float3 value,
              out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, normal);

    if (!IsAbove(mtlData, outgoingDir))
        return false;

    float NdotL = dot(normal, outgoingDir);
    pdf = NdotL * INV_PI;

    if (pdf < 0.001)
        return false;

    float NdotV = saturate(dot(normal, mtlData.V));
    float LdotV = saturate(dot(outgoingDir, mtlData.V));
    value = mtlData.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtlData.bsdfData.perceptualRoughness) * pdf;

    return true;
}

void EvaluateBurley(MaterialData mtlData,
                    float3 normal,
                    float3 outgoingDir,
                out float3 value,
                out float pdf)
{
    float NdotL = saturate(dot(normal, outgoingDir));
    float NdotV = saturate(dot(normal, mtlData.V));
    float LdotV = saturate(dot(outgoingDir, mtlData.V));

    pdf = NdotL * INV_PI;
    value = mtlData.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtlData.bsdfData.perceptualRoughness) * pdf;
}

#endif // USE_DIFFUSE_LAMBERT_BRDF

bool SampleDiffuse(MaterialData mtlData,
                   float3 normal,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    return SampleLambert(mtlData, normal, inputSample, outgoingDir, value, pdf);
#else
    return SampleBurley(mtlData, normal, inputSample, outgoingDir, value, pdf);
#endif
}

void EvaluateDiffuse(MaterialData mtlData,
                     float3 normal,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    EvaluateLambert(mtlData, normal, outgoingDir, value, pdf);
#else
    EvaluateBurley(mtlData, normal, outgoingDir, value, pdf);
#endif
}

void EvaluateSheen(MaterialData mtlData,
                   float3 normal,
                   float roughness,
                   float3 outgoingDir,
               out float3 value,
               out float pdf)
{
    // We use cosine-weighted sampling for this lobe
    float NdotL = saturate(dot(normal, outgoingDir));
    pdf = NdotL * INV_PI;

    float NdotV = dot(normal, mtlData.V);
    if (NdotV < 0.001)
    {
        value = 0.0;
        return;
    }

    roughness = clamp(roughness, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

    float3 H = normalize(mtlData.V + outgoingDir);
    float NdotH = dot(normal, H);

    float D = D_Charlie(NdotH, roughness);

    // We use this visibility term to match the raster implementation (Fabric.hlsl)
    float Vg = V_Ashikhmin(NdotL, NdotV);
    //float Vg = V_Charlie(NdotL, NdotV, roughness);

    value = mtlData.bsdfData.fresnel0 * D * Vg * NdotL;
}

} // namespace BRDF

namespace BTDF
{

bool SampleGGX(MaterialData mtlData,
               float3 normal,
               float roughness,
               float ior,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    roughness = clamp(roughness, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

    float NdotL, NdotH, VdotH;
    float3x3 localToWorld = GetLocalFrame(normal);
    SampleGGXDir(inputSample.xy, mtlData.V, localToWorld, roughness, outgoingDir, NdotL, NdotH, VdotH);

    // FIXME: won't be necessary after new version of SampleGGXDir()
    float3 H = normalize(mtlData.V + outgoingDir);
    outgoingDir = refract(-mtlData.V, H, 1.0 / ior);
    NdotL = dot(normal, outgoingDir);

    if (NdotL > -0.001 || !IsBelow(mtlData, outgoingDir))
        return false;

    float NdotV = dot(normal, mtlData.V);
    float LdotH = dot(outgoingDir, H);

    float3 F = F_Schlick(mtlData.bsdfData.fresnel0, VdotH);
    float  D = D_GGX(NdotH, roughness);
    float Vg = V_SmithJointGGX(-NdotL, NdotV, roughness);

    // Compute the Jacobian
    float jacobian = max(abs(VdotH + ior * LdotH), 0.001);
    jacobian = Sq(ior) * abs(LdotH) / Sq(jacobian);

    pdf = D * NdotH * jacobian;
    value = abs(4.0 * (1.0 - F) * D * Vg * NdotL * VdotH * jacobian);

    return pdf > 0.001;
}

bool SampleAnisoGGX(MaterialData mtlData,
                    float3 normal,
                    float roughnessX,
                    float roughnessY,
                    float ior,
                    float3 inputSample,
                out float3 outgoingDir,
                out float3 value,
                out float pdf)
{
    roughnessX = clamp(roughnessX, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);
    roughnessY = clamp(roughnessY, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

    float VdotH;
    float3 localV, localH;
    float3x3 localToWorld = GetTangentFrame(mtlData.bsdfData.tangentWS,
                                            mtlData.bsdfData.bitangentWS,
                                            normal, roughnessX != roughnessY);
    SampleAnisoGGXVisibleNormal(inputSample.xy, mtlData.V, localToWorld, roughnessX, roughnessY, localV, localH, VdotH);

    // Compute refraction direction instead of reflection
    float3 localL = refract(-localV, localH, 1.0 / ior);
    outgoingDir = mul(localL, localToWorld);

    if (localL.z > -0.001 || !IsBelow(mtlData, outgoingDir))
        return false;

    // Compute the Jacobian
    float LdotH = dot(localL, localH);
    float jacobian = max(abs(VdotH + ior * LdotH), 0.001);
    jacobian = Sq(ior) * abs(LdotH) / Sq(jacobian);

    float3 F = F_Schlick(mtlData.bsdfData.fresnel0, VdotH);
    float  D = D_AnisoGGX(roughnessX, roughnessY, localH);

    float pdfNoGV = D * VdotH * jacobian / localV.z;
    float lambdaVPlusOne = Lambda_AnisoGGX(roughnessX, roughnessY, localV) + 1.0;
    float lambdaL = Lambda_AnisoGGX(roughnessX, roughnessY, localL);

    pdf = pdfNoGV / lambdaVPlusOne;
    value = abs((1.0 - F) * pdfNoGV / (lambdaVPlusOne + lambdaL));

    return pdf > 0.001;
}

bool SampleDelta(MaterialData mtlData,
                 float3 normal,
                 float ior,
             out float3 outgoingDir,
             out float3 value,
             out float pdf)
{
    if (IsAbove(mtlData))
    {
        outgoingDir = refract(-mtlData.V, normal, 1.0 / ior);
        float NdotV = dot(normal, mtlData.V);
        value = 1.0 - F_Schlick(mtlData.bsdfData.fresnel0, NdotV);
    }
    else // Below
    {
        outgoingDir = -refract(mtlData.V, normal, ior);
        float NdotV = -dot(normal, mtlData.V);
        value = 1.0 - F_FresnelDielectric(1.0 / ior, NdotV);
    }

    value *= DELTA_PDF;
    pdf = DELTA_PDF;

    return any(outgoingDir);
}

bool SampleLambert(MaterialData mtlData,
                   float3 normal,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
    bool retVal = BRDF::SampleLambert(mtlData, normal, inputSample, outgoingDir, value, pdf);
    outgoingDir = -outgoingDir;
    return retVal;
}

void EvaluateLambert(MaterialData mtlData,
                     float3 normal,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
    BRDF::EvaluateLambert(mtlData, normal, -outgoingDir, value, pdf);
}

} // namespace BTDF

namespace SSS
{

#define MAX_WALK_STEPS 16
#define DIM_OFFSET 42
#define DIM_THIN_NORMAL_FLIP 108 // First fully available dimension

struct Result
{
    float3 throughput;
    float3 exitPosition;
    float3 exitNormal;
};

bool RandomWalk(float3 position, float3 normal, float3 diffuseColor, float3 meanFreePath, uint2 pixelCoord, out Result result, bool isThin = false)
{
    // Remap from our user-friendly parameters to and sigmaS and sigmaT
    float3 sigmaS, sigmaT;
    RemapSubSurfaceScatteringParameters(diffuseColor, meanFreePath, sigmaS, sigmaT);

    // Initialize the payload
    PathPayload payload;
    payload.segmentID = SEGMENT_ID_RANDOM_WALK;

    // Initialize the walk parameters
    RayDesc ray;
    ray.Origin = position - normal * _RayTracingRayBias;
    ray.TMin = 0.0;

    bool hit;
    uint walkIdx = 0;
    float4 walkSample;

    result.throughput = 1.0;

    do // Start our random walk
    {
        // Samples for direction, distance and channel selection
        walkSample = GetSample4D(pixelCoord, _RaytracingSampleIndex, DIM_OFFSET + 4 * walkIdx);

        // Compute the per-channel weight
        float3 weights = result.throughput * SafeDivide(sigmaS, sigmaT);

        // Normalize our weights
        float wSum = weights.x + weights.y + weights.z;
        float3 channelWeights = SafeDivide(weights, wSum);

        // Evaluate what channel we should be using for this sample
        uint channelIdx = GetChannel(walkSample[3], channelWeights);

        // Evaluate the length of our steps
        ray.TMax = -log(1.0 - walkSample[2]) / sigmaT[channelIdx];

        // Sample our next path segment direction
        ray.Direction = walkIdx ?
            SampleSphereUniform(walkSample[0], walkSample[1]) : SampleHemisphereCosine(walkSample[0], walkSample[1], -normal);

        // Initialize the payload data
        payload.rayTHit = FLT_INF;

        // Do the next step
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER | RAY_FLAG_CULL_FRONT_FACING_TRIANGLES,
                 RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, ray, payload);

        // Check if we hit something
        hit = payload.rayTHit < FLT_INF;

        // How much did the ray travel?
        float t = hit ? payload.rayTHit : ray.TMax;

        // Evaluate the transmittance for the current segment
        float3 transmittance = exp(-t * sigmaT);

        // Evaluate the pdf for the current segment
        float pdf = dot((hit ? transmittance : sigmaT * transmittance), channelWeights);

        // Contribute to the throughput
        result.throughput *= SafeDivide(hit ? transmittance : sigmaS * transmittance, pdf);

        // Compute the next path position
        ray.Origin += ray.Direction * t;

        // increment the path depth
        walkIdx++;
    }
    while (!hit && walkIdx < MAX_WALK_STEPS);

    // Set the exit intersection position and normal
    if (!hit)
    {
        result.exitPosition = position;
        result.exitNormal = normal;
        result.throughput = diffuseColor;

        // By not returning false here, we default to a diffuse BRDF when an intersection is not found;
        // this is physically wrong, but may prove more convenient for a user, as results will look
        // like diffuse instead of getting slightly darker when the mean free path becomes shorter.
        //return false;
    }
    else
    {
        result.exitPosition = ray.Origin;
        result.exitNormal = payload.value;
    }

#ifdef _DOUBLESIDED_ON

    // If we are dealing with a thin (double-sided) surface, we randomly flip the output normal half the time
    if (isThin)
    {
        if (GetSample(pixelCoord, _RaytracingSampleIndex, DIM_THIN_NORMAL_FLIP) < 0.5)
            result.exitNormal = -result.exitNormal;

        result.throughput *= 2.0;
    }

#endif

    return true;
}

} // namespace SSS

#endif // UNITY_PATH_TRACING_BSDF_INCLUDED
