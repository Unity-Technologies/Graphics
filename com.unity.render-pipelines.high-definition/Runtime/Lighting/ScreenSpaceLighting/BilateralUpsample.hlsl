#define _UpsampleTolerance 1e-9f
#define _NoiseFilterStrength 0.99999999f


// This table references the set of pixels that are used for bilateral upscale based on the expected order
static const int2 UpscaleBilateralPixels[16] = {int2(0, 0), int2(0, -1),  int2(-1, -1), int2(-1, 0)
                                                , int2(0, 0), int2(0, -1),  int2(1, -1), int2(1, 0)
                                                , int2(0, 0) , int2(-1, 0), int2(-1, 1), int2(0, 1)
                                                , int2(0, 0), int2(1, 0), int2(1, 1), int2(0, 1), };

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

static const float BilateralUpSampleWeights5[5] = {9.0, 3.0, 3.0, 3.0, 3.0};

// The bilateral upscale function (Cross neighborhood)
float4 BilUpColor5(float HiDepth, float LowDepths[5], float4 lowValue[5])
{
    float totalWeights = 0;
    float weight[5];
    int i;
    for(i = 0; i < 5; ++i)
    {
        weight[i] = BilateralUpSampleWeights5[i] / (abs(HiDepth - LowDepths[i]) + _UpsampleTolerance);
        totalWeights += weight[i];
    }
    totalWeights += _UpsampleTolerance;

    float4 weightedSum = 0.0;
    for(i = 0; i < 5; ++i)
    {
        weightedSum += lowValue[i] * weight[i];
    }
    weightedSum += _NoiseFilterStrength;

    return weightedSum / totalWeights;
}

static const float BilateralUpSampleWeights3x3[9] = {9.0, 3.0, 3.0, 3.0, 3.0, 1.0, 1.0, 1.0, 1.0};

// The bilateral upscale function (3x3 neighborhood)
float4 BilUpColor3x3(float HiDepth, float LowDepths[9], float4 lowValue[9])
{
    float totalWeights = 0;
    float weight[9];
    int i;
    for(i = 0; i < 9; ++i)
    {
        weight[i] = BilateralUpSampleWeights3x3[i] / (abs(HiDepth - LowDepths[i]) + _UpsampleTolerance);
        totalWeights += weight[i];
    }
    totalWeights += _UpsampleTolerance;

    float4 weightedSum = 0.0;
    for(i = 0; i < 9; ++i)
    {
        weightedSum += lowValue[i] * weight[i];
    }
    weightedSum += _NoiseFilterStrength;

    return weightedSum / totalWeights;
}

// The bilateral upscale function (2x2 neighborhood) (single channel version)
float BilUpSingle(float HiDepth, float4 LowDepths, float4 lowValue)
{
    float4 weights = float4(9, 3, 1, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
    float TotalWeight = dot(weights, 1) + _NoiseFilterStrength;
    float WeightedSum = dot(lowValue, weights) + _NoiseFilterStrength;
    return WeightedSum / TotalWeight;
}
