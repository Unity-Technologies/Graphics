#ifndef SPHERICAL_GAUSSIANS
#define SPHERICAL_GAUSSIANS

struct SphericalGaussian
{
    float amplitude;
    float sharpness;
    float3 mean;
};

// http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
float SGEvaluateFromDirection(in SphericalGaussian sg, const in float3 direction)
{
    // MADD optimized form of: a.amplitude * exp(a.sharpness * (dot(a.mean, direction) - 1.0));
    return sg.amplitude * exp(dot(sg.mean, direction) * sg.sharpness - sg.sharpness);
}

float SGClampedCosineWindowEvaluateFromDirection(in SphericalGaussian sg, const in float3 direction)
{
    // MADD optimized form of: a.amplitude * exp(a.sharpness * (dot(a.mean, direction) - 1.0));
    float mDotD = dot(sg.mean, direction);
    return sg.amplitude * saturate(mDotD) * exp(mDotD * sg.sharpness - sg.sharpness);
}

// https://www.desmos.com/calculator/nk5axg4tf0
float SGClampedCosineWindowIntegralFromSharpness(const in float sharpness)
{
    return exp2(2.15943 * pow(abs(sharpness * 0.0862171 + 0.620987), -1.0)) * 0.315329 + -0.297488;
}

float SGClampedCosineWindowIntegral(in SphericalGaussian sg)
{
    return sg.amplitude * SGClampedCosineWindowIntegralFromSharpness(sg.sharpness);
}

// Does not include divide by PI required to normalize the clamped cosine diffuse BRDF.
// This was done to match the format of SGIrradianceFitted() which also does not include the divide by PI.
// Post dividing by PI is required.
float SGClampedCosineWindowAndClampedCosineProductIntegral(in SphericalGaussian sg, const in float3 clampedCosineNormal)
{
    float mDotN = dot(sg.mean, clampedCosineNormal);

    float sharpnessScale = pow(abs(sg.sharpness), -0.136519) * 2.08795 + -0.635199;
    float sharpnessBias = pow(abs(sg.sharpness), 0.0798249) * 1.07557 + -1.24343;

    mDotN = mDotN * sharpnessScale + sharpnessBias;

    float res = max(0.0, exp2(-6.4923 * pow(abs(mDotN * 0.310635 + -0.864745), 4.0)) * 1.43768 + -0.00923259);
    res *= SGClampedCosineWindowIntegral(sg);
    return res;
}

// Note: Integral(SG) == SolidAngle(sg)
// http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
// http://cg.cs.tsinghua.edu.cn/people/~kun/interreflection/interreflection_paper.pdf
float SGIntegral(const in SphericalGaussian a)
{
    float b = a.amplitude * 2.0 * PI / a.sharpness;
    return exp(-2.0 * a.sharpness) * -b + b;
}

// Same as SGIntegral() but assumes unit amplitude.
float SGIntegralFromSharpness(const in float sharpness)
{
    float b = 2.0 * PI / sharpness;
    return exp(-2.0 * sharpness) * -b + b;
}

// Approximate solid angle of a spherical gaussian.
// Approximation accurate for sharpness > 5.0
// Approximation defines the solid angle of a cone, centered around the spherical gaussian
// With a COVERAGE_RATIO of 0.9, the cone covers 90% of the spherical gaussian lobe.
// http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
float SGIntegralApproximate(const in SphericalGaussian a)
{
    // Reference:
    // const float COVERAGE_RATIO = 0.9;
    // const float COVERAGE_EPSILON = 1.0 - COVERAGE_RATIO;
    // const float NEGATIVE_LOG_COVERAGE = -log(COVERAGE_EPSILON);
    // return NEGATIVE_LOG_COVERAGE * TWO_PI / sharpness;
    // NEGATIVE_LOG_COVERAGE == 1.0 with a COVERAGE_RATIO of 0.9
    return a.amplitude * 2.0 * PI / a.sharpness;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-3-diffuse-lighting-from-an-sg-light-source/
// This does not include BRDF normalization term 1 / Pi.
float SGIrradianceFitted(const in SphericalGaussian lightingLobe, const in float3 surfaceNormal)
{
    const float muDotN = dot(lightingLobe.mean, surfaceNormal);
    const float lambda = lightingLobe.sharpness;

    const float c0 = 0.36f;
    const float c1 = 1.0f / (4.0f * c0);

    float eml  = exp(-lambda);
    float em2l = eml * eml;
    float rl   = rcp(lambda);

    float scale = 1.0f + 2.0f * em2l - rl;
    float bias  = (eml - em2l) * rl - em2l;

    float x  = sqrt(1.0f - scale);
    float x0 = c0 * muDotN;
    float x1 = c1 * x;

    float n = x0 + x1;

    float y = saturate(muDotN);
    if(abs(x0) <= x1)
        y = n * n / x;

    float result = scale * y + bias;

    return result * SGIntegralApproximate(lightingLobe);
}

SphericalGaussian SphericalGaussianVectorProduct(SphericalGaussian lhs, SphericalGaussian rhs)
{
    SphericalGaussian res;
    res.mean = lhs.sharpness * lhs.mean + rhs.sharpness * rhs.mean;
    res.sharpness = length(res.mean);
    res.mean /= res.sharpness;
    res.amplitude = lhs.amplitude * rhs.amplitude * exp(res.sharpness - (lhs.sharpness + rhs.sharpness));

    return res;
}

#endif // endof SPHERICAL_GAUSSIANS
