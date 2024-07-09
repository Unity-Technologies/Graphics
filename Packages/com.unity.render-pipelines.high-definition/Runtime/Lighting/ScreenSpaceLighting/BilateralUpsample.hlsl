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

// THe bilateral upscale function (2x2 neighborhood, color3 version)
float3 BilUpColor3WithWeight(float HiDepth, float4 LowDepths, float3 lowValue0, float3 lowValue1, float3 lowValue2, float3 lowValue3, float4 initialWeight)
{
    float4 weights = initialWeight * float4(9, 3, 1, 3) / (abs(HiDepth - LowDepths) + _UpsampleTolerance);
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
    // Low resolution scene depths
    float4 lowDepthA;
    float4 lowDepthB;
    float lowDepthC;

    // The low resolution depth values
    float4 lowDepthValueA;
    float4 lowDepthValueB;
    float lowDepthValueC;

    // The low resolution color values
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

// The bilateral upscale function (3x3 neighborhood)
// Perform joint bilateral upsampling using the scene depth as guide signal
// https://bartwronski.com/2019/09/22/local-linear-models-guided-filter/
void BilUpColor3x3(float highDepth, in NeighborhoodUpsampleData3x3 data, out float4 outColor, out float outDepth)
{
    float4 combinedWeightsA = data.lowWeightA / (abs(highDepth - data.lowDepthA) + _UpsampleTolerance);
    float4 combinedWeightsB = data.lowWeightB / (abs(highDepth - data.lowDepthB) + _UpsampleTolerance);
    float combinedWeightsC = data.lowWeightC / (abs(highDepth - data.lowDepthC) + _UpsampleTolerance);

    float TotalWeight = combinedWeightsA.x + combinedWeightsA.y + combinedWeightsA.z + combinedWeightsA.w
                    + combinedWeightsB.x + combinedWeightsB.y + combinedWeightsB.z + combinedWeightsB.w
                    + combinedWeightsC;

    float4 WeightedSum = data.lowValue0 * combinedWeightsA.x
                        + data.lowValue1 * combinedWeightsA.y
                        + data.lowValue2 * combinedWeightsA.z
                        + data.lowValue3 * combinedWeightsA.w
                        + data.lowValue4 * combinedWeightsB.x
                        + data.lowValue5 * combinedWeightsB.y
                        + data.lowValue6 * combinedWeightsB.z
                        + data.lowValue7 * combinedWeightsB.w
                        + data.lowValue8 * combinedWeightsC
                        + float4(_NoiseFilterStrength.xxx, 0.0f);

    float WeightedDepth = dot(data.lowDepthValueA, combinedWeightsA)
                        + dot(data.lowDepthValueB, combinedWeightsB)
                        + data.lowDepthValueC * combinedWeightsC
                        + _NoiseFilterStrength;

    // This branch shouldn't be needed if we do the neighbor analysis mentionned in BuildRay
    if (TotalWeight == 0.0f)
    {
        outColor = float4(0, 0, 0, 1);
        outDepth = UNITY_NEAR_CLIP_VALUE;
    }
    else
    {
        outColor = WeightedSum / (TotalWeight + _NoiseFilterStrength);
        outDepth = WeightedDepth / (TotalWeight + _NoiseFilterStrength);
    }
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
