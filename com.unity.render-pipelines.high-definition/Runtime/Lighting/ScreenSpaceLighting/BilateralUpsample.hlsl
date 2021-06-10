#define _UpsampleTolerance 1e-9f
#define _NoiseFilterStrength 0.99999999f


// This table references the set of pixels that are used for bilateral upscale based on the expected order
static const int2 UpscaleBilateralPixels[16] = {int2(0, 0), int2(0, -1),  int2(-1, -1), int2(-1, 0)
                                                , int2(0, 0), int2(0, -1),  int2(1, -1), int2(1, 0)
                                                , int2(0, 0) , int2(-1, 0), int2(-1, 1), int2(0, 1)
                                                , int2(0, 0), int2(1, 0), int2(1, 1), int2(0, 1), };

static const int2 IndexToLocalOffsetCoords[9] = {int2(-1, -1), int2(0, -1),  int2(1, -1)
                                                , int2(-1, 0), int2(0, 0),  int2(1, 0)
                                                , int2(-1, 1) , int2(0, 1), int2(1, 1)};

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

static const float BilateralUpSampleWeights3x3[9] = {9.0, 3.0, 3.0, 3.0, 3.0, 1.0, 1.0, 1.0, 1.0};

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

    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.x, 0, data.lowMasksA.x, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.y, 1, data.lowMasksA.y, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.z, 2, data.lowMasksA.z, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthA.w, 3, data.lowMasksA.w, closestNeighhor, currentDistance, rejectedNeighborhood);

    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.x, 4, data.lowMasksB.x, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.y, 5, data.lowMasksB.y, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.z, 6, data.lowMasksB.z, closestNeighhor, currentDistance, rejectedNeighborhood);
    EvaluateMaskValidity(linearHighDepth, data.lowDepthB.w, 7, data.lowMasksB.w, closestNeighhor, currentDistance, rejectedNeighborhood);

    EvaluateMaskValidity(linearHighDepth, data.lowDepthC, 8, data.lowMasksC, closestNeighhor, currentDistance, rejectedNeighborhood);
}

// The bilateral upscale function (3x3 neighborhood)
float4 BilUpColor3x3(float highDepth, in NeighborhoodUpsampleData3x3 data)
{
    float4 weightsA = float4(1, 3, 1, 3) / (abs(highDepth - data.lowDepthA) + _UpsampleTolerance);
    float4 weightsB = float4(9, 3, 1, 3) / (abs(highDepth - data.lowDepthB) + _UpsampleTolerance);
    float weightsC = 1.0 / (abs(highDepth - data.lowDepthC) + _UpsampleTolerance);

    float TotalWeight = dot(weightsA, 1) + dot(weightsB, 1) + weightsC + _NoiseFilterStrength;
    float4 WeightedSum = data.lowValue0 * weightsA.x
                        + data.lowValue1 * weightsA.y
                        + data.lowValue2 * weightsA.z
                        + data.lowValue3 * weightsA.w
                        + data.lowValue4 * weightsB.x
                        + data.lowValue5 * weightsB.y
                        + data.lowValue6 * weightsB.z
                        + data.lowValue7 * weightsB.w
                        + data.lowValue8 * weightsC
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
