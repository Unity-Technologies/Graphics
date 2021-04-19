// Matrix used to move the samples from world space to local area light space in order to get the UV mapping values
float4x4 _RaytracingLightWorldToLocal;

struct MISSamplingInput
{
    // Value used for the importance sampling
    float2 noiseValue;
    // Value that defines the probability of going for either area light or BRDF sampling
    float brdfProb;
    // Flag that defines which MIS technique we should be using
    float mis;
    // Local to world value of the local pixel
    float3x3 localToWorld;
    // Roughness of the current pixel
    float roughness;
    // Position of the current pixel
    float3 positionWS;
    // View direction of the current pixel
    float3 viewWS;
    // Rectangle position
    float3 rectWSPos;
    // Dimensions of the rectangle area light
    float2 rectDimension;
};

struct MISSamplingOuput
{
    // Sampled direction
    float3 dir;
    // Sampled position
    float3 pos;
    // PDF of the brdf technique
    float brdfPDF;
    // PDF of the light technique
    float lightPDF;
    // UV coordinate on the light
    float2 sampleUV;
};

struct LightSamplingOutput
{
    // Sampled direction
    float3 dir;
    // Sampled position
    float3 pos;
    // PDF of the light technique
    float lightPDF;
};

#define ANALYTIC_RADIANCE_THRESHOLD 1e-4

// The approach here is that on a grid pattern, every pixel is using the opposite technique of his direct neighbor and every sample the technique used changes
void EvaluateMISTechnique(inout MISSamplingInput samplingInput)
{
    if (samplingInput.noiseValue.x <= samplingInput.brdfProb)
    {
        samplingInput.mis = 0.0;
        samplingInput.noiseValue.x /= samplingInput.brdfProb;
    }
    else
    {
        samplingInput.mis = 1.0;
        samplingInput.noiseValue.x = (samplingInput.noiseValue.x - samplingInput.brdfProb) / (1.0 - samplingInput.brdfProb);
    }
}

void InitSphericalQuad(LightData areaLightData, float3 positionWS, out SphQuad squad)
{
    // Dimension of the area light
    float halfWidth  = areaLightData.size.x * 0.5;
    float halfHeight = areaLightData.size.y * 0.5;

    // Compute the world space position of the center of the lightlight
    float3 areaLightPosWS = areaLightData.positionRWS;

    // Let's first compute the position of the rectangle's corners in world space
    float3 v0 = areaLightPosWS + areaLightData.right *  halfWidth + areaLightData.up *  halfHeight;
    float3 v1 = areaLightPosWS + areaLightData.right *  halfWidth + areaLightData.up * -halfHeight;
    float3 v2 = areaLightPosWS + areaLightData.right * -halfWidth + areaLightData.up * -halfHeight;
    float3 v3 = areaLightPosWS + areaLightData.right * -halfWidth + areaLightData.up *  halfHeight;

    float3 ex = v1 - v0;
    float3 ey = v3 - v0;

    SphQuadInit(v0, ex, ey, positionWS, squad);
}

bool InitSphericalQuad(LightData light, float3 positionWS, float3 normalWS, inout SphQuad squad)
{
    ZERO_INITIALIZE(SphQuad, squad);
    
    // Dimension of the area light
    float halfWidth  = light.size.x * 0.5;
    float halfHeight = light.size.y * 0.5;

    // Compute the world space position of the center of the lightlight
    float3 areaLightPosWS = light.positionRWS;

    // Let's evaluate if the point can potentially receive lighting from this point
    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    // Convert the point to the local space of the area light
    float3 positionLS = mul(positionWS - areaLightPosWS, transpose(lightToWorld));

    // Let's first compute the position of the rectangle's corners in world space
    float3 v0 = areaLightPosWS + light.right *  halfWidth + light.up *  halfHeight;
    float3 v1 = areaLightPosWS + light.right *  halfWidth + light.up * -halfHeight;
    float3 v2 = areaLightPosWS + light.right * -halfWidth + light.up * -halfHeight;
    float3 v3 = areaLightPosWS + light.right * -halfWidth + light.up *  halfHeight;

    // Make sure that this point may have light contributions
    float d = -dot(normalWS, positionWS);
    if (positionLS.z <= 0.0 || (positionLS.z > 0.0 && ((dot(normalWS, v0) + d < 0) && (dot(normalWS, v1) + d < 0) && (dot(normalWS, v2) + d < 0) && (dot(normalWS, v3) + d < 0))))
        return false;
        
    float3 ex = v1 - v0;
    float3 ey = v3 - v0;

    SphQuadInit(v0, ex, ey, positionWS, squad);
    return true;
}

float EvalBrdfPDF(MISSamplingInput misInput, float3 L)
{
    // Compute the specular PDF
    float3 H = normalize(L + misInput.viewWS );
    float NdotH = dot(misInput.localToWorld[2], H);
    float LdotH = dot(L, H);
    return D_GGX(NdotH, misInput.roughness) * NdotH / (4.0 * LdotH);
}

void brdfSampleMIS(MISSamplingInput misInput, out float3 direction, out float pdf)
{
    // Specular BRDF sampling
    float NdotL, NdotH, VdotH;
    SampleGGXDir(misInput.noiseValue, misInput.viewWS, misInput.localToWorld, misInput.roughness, direction, NdotL, NdotH, VdotH);

    // Evaluate the pdf for this sample
    pdf = EvalBrdfPDF(misInput, direction);
}

// Here we decided to use a "Damier" pattern to define which importance sampling technique to use for the MIS
bool GenerateMISSample(inout MISSamplingInput misInput, SphQuad squad, float3 viewVector, inout MISSamplingOuput misSamplingOutput)
{
    // Flag that defines if this sample is valid
    bool validity = false;

    if (misInput.mis < 0.5f)
    {
        // Compute the output light direction
        brdfSampleMIS(misInput, misSamplingOutput.dir, misSamplingOutput.brdfPDF);

        // First we need to figure out if this sample touches the area light otherwise it is not a valid sample
        float t;
        validity = IntersectPlane(misInput.positionWS, misSamplingOutput.dir, misInput.rectWSPos, squad.z, t);

        if (validity)
        {
            // Let's compute the sample pos
            misSamplingOutput.pos = misInput.positionWS + t * misSamplingOutput.dir;

            // The next question is: This the sample point inside the triangle? To do that for the moment we move it to the local space of the light and see if its distance to the center of the light
            // is coherent with the dimensions of the light
            float4 lsPoint = mul(_RaytracingLightWorldToLocal, float4(misSamplingOutput.pos, 1.0)) * 2.0;
            validity = abs(lsPoint.x) < misInput.rectDimension.x && abs(lsPoint.y) < misInput.rectDimension.y;
            if (validity)
            {
                // Compute the uv on the light
                misSamplingOutput.sampleUV = float2((lsPoint.x + misInput.rectDimension.x) / (2.0 * misInput.rectDimension.x), (lsPoint.y + misInput.rectDimension.y) /  (2.0 * misInput.rectDimension.y));
                // Compute the Light PDF
                misSamplingOutput.lightPDF = 1.0f / squad.S;
            }
        }
    }
    else
    {
        validity =  true;

        misSamplingOutput.pos = SphQuadSample(squad, misInput.noiseValue.x, misInput.noiseValue.y);
        misSamplingOutput.dir = normalize(misSamplingOutput.pos - misInput.positionWS);

        misSamplingOutput.brdfPDF = EvalBrdfPDF(misInput, misSamplingOutput.dir);
        // Compute the Light PDF
        misSamplingOutput.lightPDF = 1.0f / squad.S;
        // Compute the uv on the light
        float4 lsPoint = mul(_RaytracingLightWorldToLocal, float4(misSamplingOutput.pos, 1.0)) * 2.0;
        misSamplingOutput.sampleUV = float2((lsPoint.x + misInput.rectDimension.x) / (2.0 * misInput.rectDimension.x), (lsPoint.y + misInput.rectDimension.y) /  (2.0 * misInput.rectDimension.y));
    }
    return validity;
}

void GenerateLightSample(float3 positionWS, float2 theSample, SphQuad squad, float3 viewVector, out LightSamplingOutput lightSamplingOutput)
{
    lightSamplingOutput.pos = SphQuadSample(squad, theSample.x, theSample.y);
    lightSamplingOutput.dir = normalize(lightSamplingOutput.pos - positionWS);
    lightSamplingOutput.lightPDF = 1.0f / squad.S;
}

bool EvaluateMISProbabilties(DirectLighting lighting, float perceptualRoughness, out float brdfProb)
{
    // Compute the magnitude of both the diffuse and specular terms
    const float diffRadiance  = Luminance(lighting.diffuse);
    const float specRadiance  = Luminance(lighting.specular);
    const float totalRadiance = diffRadiance + specRadiance;

    // The exact factor to attenuate the brdf probability using the perceptualRoughness has been experimentally defined. It requires
    // an other pass to see if we can improve the quality if we use an other mapping (roughness instead of perceptualRoughness for instance)
    brdfProb = specRadiance / max(totalRadiance, ANALYTIC_RADIANCE_THRESHOLD);
    brdfProb = lerp(brdfProb, 0.0, perceptualRoughness);

    return totalRadiance > ANALYTIC_RADIANCE_THRESHOLD;
}
