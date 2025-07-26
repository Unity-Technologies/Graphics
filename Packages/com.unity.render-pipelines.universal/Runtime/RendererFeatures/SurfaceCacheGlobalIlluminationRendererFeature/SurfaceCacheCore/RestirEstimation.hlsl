#define SAMPLE_TYPE_HIT 0
#define SAMPLE_TYPE_ENV 1

struct Sample
{
    float3 radiance;
    float3 hitPointOrDirection;
    float3 rayOrigin;
    float3 rayDirection;
    uint sampleType; // SAMPLE_TYPE_HIT is hit, SAMPLE_TYPE_ENV is env.
};

struct Realization
{
    Sample sample;
    float weight;
    float confidence;
};

float TargetFunction(Sample sample)
{
    return sample.radiance.r + sample.radiance.g + sample.radiance.b;
}

struct Reservoir
{
    Sample sample;
    float weightSum;
    float confidenceSum;

    void Init()
    {
        sample = (Sample)0;
        weightSum = 0.0f;
        confidenceSum = 0;
    }

    void Update(in Sample newSample, float confidence, float weight, float u)
    {
        confidenceSum += confidence;
        weightSum += weight;
        if (u * weightSum < weight)
            sample = newSample;
    }
};

Realization CreateRealizationFromReservoir(in Reservoir reservoir)
{
    Realization realization;
    realization.sample = reservoir.sample;
    realization.confidence = reservoir.confidenceSum;
    if (reservoir.weightSum == 0.0f)
        realization.weight = 0.0f;
    else
        realization.weight = reservoir.weightSum / (TargetFunction(reservoir.sample) * reservoir.confidenceSum);
    return realization;
}
