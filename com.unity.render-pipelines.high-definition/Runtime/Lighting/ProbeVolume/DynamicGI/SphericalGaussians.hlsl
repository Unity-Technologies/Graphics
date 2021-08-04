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


#endif // endof SPHERICAL_GAUSSIANS
