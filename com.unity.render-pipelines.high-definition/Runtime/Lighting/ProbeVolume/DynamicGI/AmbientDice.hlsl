#ifndef AMBIENT_DICE
#define AMBIENT_DICE

struct AmbientDice
{
    float amplitude;
    float sharpness;
    float3 mean;
};

float AmbientDiceEvaluateFromDirection(in AmbientDice a, const in float3 direction)
{
    return a.amplitude * pow(saturate(dot(a.mean, direction)), a.sharpness);
}

struct AmbientDiceWrapped
{
    float amplitude;
    float sharpness;
    float3 mean;
};

float AmbientDiceWrappedEvaluateFromDirection(in AmbientDiceWrapped a, const in float3 direction)
{
    return a.amplitude * pow(saturate(dot(a.mean, direction) * 0.5 + 0.5), a.sharpness);
}

// Analytic function fit over the sharpness range of [2, 32]
// https://www.desmos.com/calculator/gl9lomqucs
float AmbientDiceIntegralFromSharpness(const in float sharpness)
{
    return exp2(6.14741 * pow(abs(sharpness * 12.5654 + 14.6469), -1.0)) * 18.5256 + -18.5238;
}

float AmbientDiceIntegral(const in AmbientDice a)
{
    return a.amplitude * AmbientDiceIntegralFromSharpness(a.sharpness);
}

// Does not include divide by PI required to normalize the clamped cosine diffuse BRDF.
// This was done to match the format of SGIrradianceFitted() which also does not include the divide by PI.
// Post dividing by PI is required.
float AmbientDiceAndClampedCosineProductIntegral(const in AmbientDice a, const in float3 clampedCosineNormal)
{
    float mDotN = dot(a.mean, clampedCosineNormal);

    float sharpnessScale = pow(abs(a.sharpness), -0.121796) * -2.18362 + 2.7562;
    float sharpnessBias = pow(abs(a.sharpness), 0.137288) * -0.555517 + 0.711175;

    mDotN = mDotN * sharpnessScale + sharpnessBias;

    float res = max(0.0, exp2(-9.45649 * pow(abs(mDotN * 0.240416 + 1.16513), -5.16291)) * 2.71745 + -0.00193676);
    res *= AmbientDiceIntegral(a);
    return res;
}

// https://www.desmos.com/calculator/umjtgtzmk8
// Fit to pre-deringed data.
// The dering constraint is evaluated post diffuse brdf convolution.
// The signal can still ring in raw irradiance space.
float ComputeZonalHarmonicC0FromAmbientDiceSharpness(float sharpness)
{
    return pow(abs(sharpness * 1.62301 + 1.59682), -0.993255) * 2.83522 + -0.001;
}

float ComputeZonalHarmonicC1FromAmbientDiceSharpness(float sharpness)
{
    return exp2(3.37607 * pow(abs(sharpness * 1.45269 + 6.46623), -1.88874)) * 20.0 + -19.9337;
}


float ComputeZonalHarmonicC2FromAmbientDiceSharpness(float sharpness)
{
    float lhs = 0.239989 + 0.42846 * sharpness + -0.202951 * sharpness * sharpness + 0.0303908 * sharpness * sharpness * sharpness;
    float rhs = exp2(-1.44747 * pow(abs(sharpness * 0.644014 + -0.188877), -0.94422)) * -0.970862 + 0.967661;
    return sharpness > 2.33 ? rhs : lhs;
}

float3 ComputeZonalHarmonicFromAmbientDiceSharpness(float sharpness)
{
    return float3(
        ComputeZonalHarmonicC0FromAmbientDiceSharpness(sharpness),
        ComputeZonalHarmonicC1FromAmbientDiceSharpness(sharpness),
        ComputeZonalHarmonicC2FromAmbientDiceSharpness(sharpness)
    );
}

#endif
