#define _UpsampleTolerance 1e-5f
#define _NoiseFilterStrength 0.99999999f


// This table references the set of pixels that are used for bilateral upscale based on the expected order
static const int2 UpscaleBilateralPixels[16] = {int2(0, 0), int2(0, -1),  int2(-1, -1), int2(-1, 0)
                                                , int2(0, 0), int2(0, -1),  int2(1, -1), int2(1, 0)
                                                , int2(0, 0) , int2(-1, 0), int2(-1, 1), int2(0, 1)
                                                , int2(0, 0), int2(1, 0), int2(1, 1), int2(0, 1), };

static const int2 IndexToLocalOffsetCoords[9] = {int2(-1, -1), int2(0, -1),  int2(1, -1)
                                                , int2(-1, 0), int2(0, 0),  int2(1, 0)
                                                , int2(-1, 1) , int2(0, 1), int2(1, 1)};

// The bilateral upscale function (2x2 neighborhood, color3 version), uniform weight version
float3 BilUpColor3_Uniform(float HiDepth, float4 LowDepths, float3 lowValue0, float3 lowValue1, float3 lowValue2, float3 lowValue3)
{
    float4 weights = float4(3, 3, 3, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float3 WeightedSum = lowValue0 * weights.x
                        + lowValue1 * weights.y
                        + lowValue2 * weights.z
                        + lowValue3 * weights.w
                        + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}

// THe bilateral upscale function (2x2 neighborhood, color3 version)
float3 BilUpColor3(float HiDepth, float4 LowDepths, float3 lowValue0, float3 lowValue1, float3 lowValue2, float3 lowValue3)
{
    float4 weights = float4(9, 3, 1, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float3 WeightedSum = lowValue0 * weights.x
                        + lowValue1 * weights.y
                        + lowValue2 * weights.z
                        + lowValue3 * weights.w
                        + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}

// The bilateral upscale function (2x2 neighborhood, color4 version)
float4 BilUpColor(float HiDepth, float4 LowDepths, float4 lowValue0, float4 lowValue1, float4 lowValue2, float4 lowValue3)
{
    float4 weights = float4(9, 3, 1, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float4 WeightedSum = lowValue0 * weights.x
                        + lowValue1 * weights.y
                        + lowValue2 * weights.z
                        + lowValue3 * weights.w
                        + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}

// The bilateral upscale function (2x2 neighborhood) (single channel version)
float BilUpSingle(float HiDepth, float4 LowDepths, float4 lowValue)
{
    float4 weights = float4(9, 3, 1, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float WeightedSum = dot(lowValue, weights) + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}

// The bilateral upscale function (2x2 neighborhood) (single channel version), uniform version
float BilUpSingle_Uniform(float HiDepth, float4 LowDepths, float4 lowValue)
{
    float4 weights = float4(3, 3, 3, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float WeightedSum = dot(lowValue, weights) + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}

// Due to compiler issues, it is not possible to use arrays to store the neighborhood values, we then store
// them in this structure
struct NeighborhoodUpsampleData3x3
{
    // Low resolution depths
    float4 lowDepthA;
    float4 lowDepthB;
    float lowDepthC;

    // The low resolution masks
    float4 lowMasksA;
    float4 lowMasksB;
    float lowMasksC;

    // The low resolution values
    float4 lowValue0;
    float4 lowValue1;
    float4 lowValue2;
    float4 lowValue3;
    float4 lowValue4;
    float4 lowValue5;
    float4 lowValue6;
    float4 lowValue7;
    float4 lowValue8;

    // Weights used to combine the neighborhood
    float4 lowWeightA;
    float4 lowWeightB;
    float lowWeightC;
};

void EvaluateMaskValidity(float linearHighDepth, float lowDepth, int currentIndex,
                            inout float inputMask, inout int closestNeighhor,
                            inout float currentDistance, inout float rejectedNeighborhood)
{
    if(inputMask == 0.0f)
        return;

    // Convert the depths to linear
    float candidateLinearDepth = Linear01Depth(lowDepth, _ZBufferParams);

    // Compute the distance between the two values
    float candidateDistance = abs(linearHighDepth - candidateLinearDepth);

    // Evaluate if this becomes the closest neighbor
    if (candidateDistance < currentDistance)
    {
        closestNeighhor = currentIndex;
        currentDistance = candidateDistance;
    }

    bool validSample = candidateDistance < (linearHighDepth * 0.3);
    inputMask = validSample ? 1.0f : 0.0f;
    rejectedNeighborhood *= (validSample ? 0.0f : 1.0f);
}

void OverrideMaskValues(float highDepth, inout NeighborhoodUpsampleData3x3 data,
                        out float rejectedNeighborhood, out int closestNeighhor)
{
    // First of all compute the linear version of the high depth
    float linearHighDepth = Linear01Depth(highDepth, _ZBufferParams);

    // Flag that tells us which pixel holds valid information
    rejectedNeighborhood = 1.0f;
    closestNeighhor = 4;
    float currentDistance = 1.0f;

    // The center has precedence over the other pixels
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.x, 4, data.lowMasksB.x, closestNeighhor, currentDistance, rejectedNeighborhood);

    // Then the plus
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.y, 1, data.lowMasksA.y, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.w, 3, data.lowMasksA.w, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.y, 5, data.lowMasksB.y, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.w, 7, data.lowMasksB.w, closestNeighhor, currentDistance, rejectedNeighborhood);

    // Then the cross
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.x, 0, data.lowMasksA.x, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.z, 2, data.lowMasksA.z, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.z, 6, data.lowMasksB.z, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthC, 8, data.lowMasksC, closestNeighhor, currentDistance, rejectedNeighborhood);
}

// The bilateral upscale function (3x3 neighborhood)
float4 BilUpColor3x3(float highDepth, in NeighborhoodUpsampleData3x3 data)
{
    float4 combinedWeightsA = data.lowWeightA / (abs(highDepth - data.lowDepthA) + _UpsampleTolerance);
    float4 combinedWeightsB = data.lowWeightB / (abs(highDepth - data.lowDepthB) + _UpsampleTolerance);
    float combinedWeightsC = data.lowWeightC / (abs(highDepth - data.lowDepthC) + _UpsampleTolerance);

    float TotalWeight = combinedWeightsA.x + combinedWeightsA.y + combinedWeightsA.z + combinedWeightsA.w
                    + combinedWeightsB.x + combinedWeightsB.y + combinedWeightsB.z + combinedWeightsB.w
                    + combinedWeightsC
                    + _NoiseFilterStrength;

    float4 WeightedSum = data.lowValue0 * combinedWeightsA.x
                        + data.lowValue1 * combinedWeightsA.y
                        + data.lowValue2 * combinedWeightsA.z
                        + data.lowValue3 * combinedWeightsA.w
                        + data.lowValue4 * combinedWeightsB.x
                        + data.lowValue5 * combinedWeightsB.y
                        + data.lowValue6 * combinedWeightsB.z
                        + data.lowValue7 * combinedWeightsB.w
                        + data.lowValue8 * combinedWeightsC
                        + float4(_NoiseFilterStrength, _NoiseFilterStrength, _NoiseFilterStrength, 0.0);
    return WeightedSum / TotalWeight;
}

// Due to compiler issues, it is not possible to use arrays to store the neighborhood values, we then store them in this structure
struct NeighborhoodUpsampleData2x2_RGB
{
    // Low resolution depths
    float4 lowDepth;

    // The low resolution values
    float3 lowValue0;
    float3 lowValue1;
    float3 lowValue2;
    float3 lowValue3;

    // Weights used to combine the neighborhood
    float4 lowWeight;
};

// The bilateral upscale function (3x3 neighborhood)
float3 BilUpColor2x2_RGB(float highDepth, in NeighborhoodUpsampleData2x2_RGB data)
{
    float4 combinedWeights = data.lowWeight / (abs(highDepth - data.lowDepth) + _UpsampleTolerance);
    float TotalWeight = combinedWeights.x + combinedWeights.y + combinedWeights.z + combinedWeights.w + _NoiseFilterStrength;
    float3 WeightedSum = data.lowValue0.xyz * combinedWeights.x
                        + data.lowValue1.xyz * combinedWeights.y
                        + data.lowValue2.xyz * combinedWeights.z
                        + data.lowValue3.xyz * combinedWeights.w
                        + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}
