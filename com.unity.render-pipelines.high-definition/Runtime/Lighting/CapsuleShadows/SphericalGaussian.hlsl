#ifndef SPHERICAL_GAUSSIAN_H
#define SPHERICAL_GAUSSIAN_H

// References:
// http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
// http://cg.cs.tsinghua.edu.cn/people/~kun/interreflection/interreflection_paper.pdf
// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-1-a-brief-and-incomplete-history-of-baked-lighting-representations/
// http://graphicrants.blogspot.com/2018/05/spherical-gaussian-series.html

struct SphericalGaussian
{
    float amplitude;
    float sharpness;
    float3 normal;
};

struct AnisotropicSphericalGaussian
{
    float amplitude;
    float2 sharpness;
    float3 normal;
    float3 tangent;
    float3 bitangent;
};

float SphericalGaussianEvaluate(SphericalGaussian lhs, float3 direction)
{
    // MADD optimized form of: lhs.amplitude * exp(lhs.sharpness * (dot(lhs.normal, direction) - 1.0));
    return lhs.amplitude * exp(dot(lhs.normal, direction) * lhs.sharpness - lhs.sharpness);
}

// http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf

float SphericalGaussianIntegral(SphericalGaussian lhs)
{
    float b = lhs.amplitude * 2.0 * PI / lhs.sharpness;
    return exp(-2.0 * lhs.sharpness) * -b + b;
}

// Same as SphericalGaussianIntegral() but assumes unit amplitude.
float SphericalGaussianIntegralNormalized(float sharpness)
{
    float b = 2.0 * PI / sharpness;
    return exp(-2.0 * sharpness) * -b + b;
}

// Approximate solid angle of a spherical gaussian.
// Approximation accurate for sharpness > 5.0
// Approximation defines the solid angle of a cone, centered around the spherical gaussian
// With a COVERAGE_RATIO of 0.9, the cone covers 90% of the spherical gaussian lobe.
float SphericalGaussianIntegralApproximate(SphericalGaussian lhs)
{
    // Reference:
    // const float COVERAGE_RATIO = 0.9;
    // const float COVERAGE_EPSILON = 1.0 - COVERAGE_RATIO;
    // const float NEGATIVE_LOG_COVERAGE = -log(COVERAGE_EPSILON);
    // return NEGATIVE_LOG_COVERAGE * TWO_PI / sharpness;
    // NEGATIVE_LOG_COVERAGE == 1.0 with a COVERAGE_RATIO of 0.9
    return lhs.amplitude * 2.0 * PI / lhs.sharpness;
}

float SphericalGaussianSolidAngleInverseApproximate(float sharpness)
{
    const float TWO_PI_INVERSE = 1.0 / (2.0 * PI);
    sharpness * TWO_PI_INVERSE;
}

// Compute a spherical gaussian that evaluates to >= epsilon within a specified cone angle.
// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-2-spherical-gaussians-101/
float SphericalGaussianSharpnessFromAngleAndThreshold(float cosTheta, float amplitude, float epsilon)
{
    return (log(epsilon) - log(amplitude)) / (cosTheta - 1.0f);
}

float SphericalGaussianSharpnessFromAngleAndLogThreshold(float cosTheta, float logAmplitude, float logEpsilon)
{
    return (logEpsilon - logAmplitude) / (cosTheta - 1.0f);
}

float SphericalGaussianInnerProduct(SphericalGaussian lhs, SphericalGaussian rhs)
{
    float sharpnessC = length(lhs.sharpness * lhs.normal + rhs.sharpness * rhs.normal);
    float amplitudeC = lhs.amplitude * rhs.amplitude * exp(sharpnessC - (lhs.sharpness + rhs.sharpness));
    return amplitudeC * SphericalGaussianIntegralNormalized(sharpnessC);
}

SphericalGaussian SphericalGaussianVectorProduct(SphericalGaussian lhs, SphericalGaussian rhs)
{
    SphericalGaussian res;
    res.normal = lhs.sharpness * lhs.normal + rhs.sharpness * rhs.normal;
    res.sharpness = length(res.normal);
    res.normal /= res.sharpness;
    res.amplitude = lhs.amplitude * rhs.amplitude * exp(res.sharpness - (lhs.sharpness + rhs.sharpness));

    return res;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-3-diffuse-lighting-from-an-sg-light-source/
SphericalGaussian SphericalGaussianFromDiffuseBRDFApproximate(float3 normal)
{
    SphericalGaussian res;
    res.normal = normal;
    res.sharpness = 2.133f;
    res.amplitude = 1.17f / PI;

    return res;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-3-diffuse-lighting-from-an-sg-light-source/
float SphericalGaussianAndProjectedAreaProductIntegralApproximateHill(SphericalGaussian sgLight, float3 normal)
{
    const float muDotN = dot(sgLight.normal, normal);
    const float lambda = sgLight.sharpness;
 
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
 
    return result * SphericalGaussianIntegralApproximate(sgLight);
}

// Approximate hemispherical integral of an SG / 2pi.
// The parameter "cosine" is the cosine of the angle between the SG axis and pole axis of the hemisphere.
// Meder and Bruderlin 2018 "Hemispherical Gausians for Accurate Lighting Integration"
float SphericalGaussianIntegralOverTwoPi(float sharpness, float cosine)
{
    // This function approximately computes the integral using an interpolation between the upper hemispherical integral and lower hemispherical integral.
    // First we compute the interpolation factor.
    // Unlike the paper, we use reciprocals of exponential functions obtained by negative exponents for the numerical stability.
    float t = sqrt(sharpness) * sharpness * (-1.6988f * sharpness - 10.8438f) / ((sharpness + 6.2201f) * sharpness + 10.2415f);
    float u = t * cosine;
    float a = exp(t);
    float b = exp(u);
    float c = 1.0f - exp(t + u); // This is equivalent to 1 - a*b but more numerically stable.
    float s = c / max(c - a + b, FLT_MIN); // We clamp the denominator to avoid zero divide for sharpness -> 0.

    // For the numerical stability, we clamp the sharpness with a small threshold.
    const float THRESHOLD = 1e-11f;
    float sharpnessClamped = max(sharpness, THRESHOLD);
    float e = exp(-sharpnessClamped);

    // Interpolation between the upper hemispherical integral and lower hemispherical integral.
    // Upper hemispherical integral: 2pi*(1 - e)/sharpnessClamped.
    // Lower hemispherical integral: 2pi*e*(1 - e)/sharpnessClamped.
    // Since this function returns the integral devided by 2pi, 2pi is eliminated from the code.
    return lerp(e, 1.0f, s) * (1.0f - e) / sharpnessClamped;
}


// Meder and Bruderlin 2018 "Hemispherical Gausians for Accurate Lighting Integration"
float SphericalGaussianAndProjectedAreaProductIntegralApproximateMeder(SphericalGaussian sgLight, float3 normal)
{
    SphericalGaussian sgLambert;
    sgLambert.normal = normal;
    sgLambert.sharpness = 0.0315f;
    sgLambert.amplitude = 1.0f;
    SphericalGaussian lobe = SphericalGaussianVectorProduct(sgLight, sgLambert);
    float integral0 = (32.7080f * 2.0f) * SphericalGaussianIntegralOverTwoPi(lobe.sharpness, dot(lobe.normal, normal)) * sgLight.amplitude * lobe.amplitude;
    float integral1 = (31.7003f * 2.0f) * SphericalGaussianIntegralOverTwoPi(sgLight.sharpness, dot(sgLight.normal, normal)) * sgLight.amplitude;

    return integral0 - integral1;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
SphericalGaussian SphericalGaussianFromNDFApproximate(float3 normal, float roughness)
{
    SphericalGaussian sgNDF;
    sgNDF.normal = normal;
    float m2 = roughness * roughness;
    sgNDF.sharpness = 2.0f / m2;
    sgNDF.amplitude = 1.0f / (PI * m2);

    return sgNDF;
}

// Warp our SG NDF from half vector space, into world space.
// This results in a normalization, and rotation, but no stretching, because we still only return an SG, not an ASG.
// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
SphericalGaussian SphericalGaussianWarpWSFromHS(in SphericalGaussian sgNDFHalfVectorSpace, float3 V)
{
    SphericalGaussian sgNDFWorldSpace;

    // TODO: Pass this in, instead of recalculating here.
    float NdotV = max(1e-5f, abs(dot(sgNDFHalfVectorSpace.normal, V)));
 
    sgNDFWorldSpace.normal = reflect(-V, sgNDFHalfVectorSpace.normal);
    sgNDFWorldSpace.amplitude = sgNDFHalfVectorSpace.amplitude;
    sgNDFWorldSpace.sharpness = sgNDFHalfVectorSpace.sharpness;
    sgNDFWorldSpace.sharpness /= (4.0f * NdotV);
 
    return sgNDFWorldSpace;
}

float AnisotropicSphericalGaussianEvaluate(AnisotropicSphericalGaussian lhs, float3 direction)
{
    float dDotN = dot(lhs.normal, direction);
    float dDotT = dot(lhs.tangent, direction);
    float dDotB = dot(lhs.bitangent, direction);

    return lhs.amplitude * max(0.0, dDotN) * exp(-lhs.sharpness.x * dDotT * dDotT - lhs.sharpness.y * dDotB * dDotB);
}

// Approximate solid angle of anisotropic spherical gaussian.
// Accurate to += 1e-5 for sharpness values ~>= 5.0
// For sharpness values < 5.0, use more accurate form below.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float AnisotropicSphericalGaussianSolidAngleApproximate(float2 sharpness)
{
    return PI * rsqrt(sharpness.x * sharpness.y);
}

// Approximate integral of anisotropic spherical gaussian.
// See AnisotropicSphericalGaussianSolidAngleApproximate for details.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float AnisotropicSphericalGaussianIntegralApproximate(AnisotropicSphericalGaussian asg)
{
    return asg.amplitude * AnisotropicSphericalGaussianSolidAngleApproximate(asg.sharpness);
}

// Rational approximation of bessel weight function for use in analytic anisotropic spherical gaussian intregral.
// http://cg.cs.tsinghua.edu.cn/people/~kun/asg/supplemental_asg.pdf
// Degenerates to 2 * PI when x == 0.0
float AnisotropicSphericalGaussianBesselWeight(float x)
{
    const float p1 = 0.7846;
    const float p2 = 3.185;
    const float p3 = 8.775;
    const float p4 = 51.51;
    const float q1 = 0.2126;
    const float q2 = 0.808;
    const float q3 = 1.523;
    const float q4 = 1.305;

    float x2 = x * x;
    float x3 = x * x2;
    float x4 = x * x3;

    return sqrt((p1 * x3 + p2 * x2 + p3 * x + p4) / (x4 + q1 * x3 + q2 * x2 + q3 * x + q4));
}

// Solid angle of anisotropic spherical gaussian.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float AnisotropicSphericalGaussianSolidAngle(float2 sharpness)
{
    float a = sharpness.x - sharpness.y;
    float b = a / sharpness.y;
    float c = AnisotropicSphericalGaussianBesselWeight(a) + b * AnisotropicSphericalGaussianBesselWeight(a + b);
    float d = exp(-sharpness.y) * c / (2.0f * sharpness.x);

    return AnisotropicSphericalGaussianSolidAngleApproximate(sharpness) - d;
}

// Integral of anisotropic spherical gaussian.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float AnisotropicSphericalGaussianIntegral(AnisotropicSphericalGaussian asg)
{
    return asg.amplitude * AnisotropicSphericalGaussianSolidAngle(asg.sharpness);
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
float AnisotropicSphericalGaussianInnerProductSG(AnisotropicSphericalGaussian asg, SphericalGaussian sg)
{
    // The ASG paper specifes an isotropic SG as
    // exp(2 * nu * (dot(v, axis) - 1)),
    // so we must divide our SG sharpness by 2 in order
    // to get the nup parameter expected by the ASG formula
    float nu = sg.sharpness * 0.5f;

    AnisotropicSphericalGaussian convolveASG;
    convolveASG.normal = asg.normal;
    convolveASG.tangent = asg.tangent;
    convolveASG.bitangent = asg.bitangent;

    convolveASG.sharpness.x = (nu * asg.sharpness.x) / (nu + asg.sharpness.x);
    convolveASG.sharpness.y = (nu * asg.sharpness.y) / (nu + asg.sharpness.y);

    convolveASG.amplitude = PI * rsqrt((nu + asg.sharpness.x) * (nu + asg.sharpness.y));

    return AnisotropicSphericalGaussianEvaluate(convolveASG, sg.normal) * sg.amplitude * asg.amplitude;
}

AnisotropicSphericalGaussian AnisotropicSphericalGaussianWarpWSFromHS(SphericalGaussian sgNDFHalfVectorSpace, float3 V)
{
    AnisotropicSphericalGaussian asgNDFWorldSpace;

    // TODO: Should be passed in rather than recalculated.
    float NdotV = max(1e-5f, abs(dot(V, sgNDFHalfVectorSpace.normal)));

    // Generate any orthonormal basis with Z pointing in the
    // direction of the reflected view vector
    asgNDFWorldSpace.normal = reflect(-V, sgNDFHalfVectorSpace.normal);
    asgNDFWorldSpace.tangent = (NdotV < 0.9999)
        ? normalize(cross(sgNDFHalfVectorSpace.normal, asgNDFWorldSpace.normal))
        : (asgNDFWorldSpace.normal.y < 0.9999)
            ? float3(0.0, 1.0, 0.0)
            : float3(1.0, 0.0, 0.0);
    asgNDFWorldSpace.bitangent = cross(asgNDFWorldSpace.normal, asgNDFWorldSpace.tangent);

    // Second derivative of the sharpness with respect to how
    // far we are from basis Axis direction
    const float SHARPNESS_MAX = 100000.0f;
    asgNDFWorldSpace.sharpness.x = min(SHARPNESS_MAX, sgNDFHalfVectorSpace.sharpness * 0.125f / (NdotV * NdotV));
    asgNDFWorldSpace.sharpness.y = min(SHARPNESS_MAX, sgNDFHalfVectorSpace.sharpness * 0.125f);

    // Slightly lerp toward an isotropic distribution to avoid precision issues near degenerate distributions.
    float sharpnessIsotropic = asgNDFWorldSpace.sharpness.x * 0.5f + asgNDFWorldSpace.sharpness.y * 0.5f;
    asgNDFWorldSpace.sharpness = lerp(asgNDFWorldSpace.sharpness, float2(sharpnessIsotropic, sharpnessIsotropic), 0.1f);

    asgNDFWorldSpace.amplitude = sgNDFHalfVectorSpace.amplitude;

    return asgNDFWorldSpace;
}

float3x3 CovarianceMatrixFromVariance(float3 variance)
{
    float3x3 res;

    float xx = variance.x * variance.x;
    float xy = variance.x * variance.y;
    float xz = variance.x * variance.z;
    float yy = variance.y * variance.y;
    float yz = variance.y * variance.z;
    float zz = variance.z * variance.z;

    res[0][0] = xx;
    res[0][1] = xy;
    res[0][2] = xz;

    res[1][0] = xy;
    res[1][1] = yy;
    res[1][2] = yz;

    res[2][0] = xz;
    res[2][1] = yz;
    res[2][2] = zz;

    return res;
}

float3 EigenVectorFromMatrix(float3x3 m)
{
    float3 res;
    float vMin;
  
    for (int i = 0; i < 3; ++i)
    {
        int pi = (i == 0) ? 2 : i - 1;
        int ni = (i == 2) ? 0 : i + 1;

        float3 c = normalize(cross(m[i], m[ni]));
        float vol = abs(dot(c, m[pi]));

        if (i == 0)
        {
            vMin = vol;
            res = c;
        }
        else if (vol < vMin)
        {
            vMin = vol;
            res = c;
        }
    }

  return res;
}

float3 EigenValuesFromMatrix(float3x3 m)
{
    float3 res;

    float b = -m[0][0] - m[1][1] - m[2][2];

    float c = -m[0][1] * m[0][1] 
        - m[0][2] * m[0][2] 
        - m[1][2] * m[1][2]
        + m[2][2] * (m[0][0] + m[1][1]) 
        + m[0][0] * m[1][1];

    float d = m[2][2] * m[0][1] * m[0][1] 
        - 2.0 * m[0][1] * m[0][2] * m[1][2] 
        + m[1][1] * m[0][2] * m[0][2] 
        + m[0][0] * m[1][2] * m[1][2]
        - m[0][0] * m[1][1] * m[2][2];

    float b2 = b * b;
    float b3 = b2 * b;

    float alpha = -b3 / 27.0 - d * 0.5 + b * c / 6.0;
    float beta = c / 3.0 - b2 / 9.0;
    float t1 = -b / 3.0;
    float t2 = 2.0 * sqrt(abs(beta));
    float ac = acos(alpha / pow(abs(-beta), 1.5) * 0.99999);

    res.x = t1 + t2 * cos(ac / 3.0);
    res.y = t1 + t2 * cos((ac + 2.0 * PI) / 3.0);
    res.z = t1 + t2 * cos((ac + 4.0 * PI) / 3.0);

    // Sort eigen values.
    res.xy = res.y < res.x ? res.yx : res.xy;
    res.yz = res.z < res.y ? res.zy : res.yz;
    res.xy = res.y < res.x ? res.yx : res.xy;

    return res;
}

float3 EigenValuesFromMatrix(float3x3 m, float eMax)
{
    float3 res;

    float bX = -m[0][0] - m[1][1] - m[2][2];

    float cX = 
        -m[0][1] * m[0][1] 
        - m[0][2] * m[0][2] 
        - m[1][2] * m[1][2]
        + m[2][2] * (m[0][0] + m[1][1]) 
        + m[0][0] * m[1][1];

    float b = bX + eMax;
    float c = cX + eMax * b;

    float deltaSqrtDiv2 = 0.5 * sqrt(abs(-4.0 * c + b * b));

    res.x = (b * -0.5 - deltaSqrtDiv2);
    res.y = (b * -0.5 + deltaSqrtDiv2);
    res.z = eMax;

    // Sort eigen values.
    res.xy = res.y < res.x ? res.yx : res.xy;
    res.yz = res.z < res.y ? res.zy : res.yz;
    res.xy = res.y < res.x ? res.yx : res.xy;

    return res;
}

// Compose standard, geometric representation of anisotropic spherical gaussian
// to the mathematically equivalent 3x3 matrix representation.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float3x3 AnisotropicSphericalGaussianToMatrix(AnisotropicSphericalGaussian asg)
{
    float eigenValueNormal = log(asg.amplitude);

    const float3x3 IDENTITY = float3x3(
        float3(1.0f, 0.0f, 0.0f),
        float3(0.0f, 1.0f, 0.0f),
        float3(0.0f, 0.0f, 1.0f)
    );

    return (-asg.sharpness.x * CovarianceMatrixFromVariance(asg.tangent))
        + (-asg.sharpness.y * CovarianceMatrixFromVariance(asg.bitangent))
        + (eigenValueNormal * IDENTITY);
}

// Decompose 3x3 matrix anisotropic spherical gaussian
// representation to mathematically equivalent geometric representation.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
AnisotropicSphericalGaussian AnisotropicSphericalGaussianFromMatrix(float3x3 asgMatrix)
{
    AnisotropicSphericalGaussian res;

    float3 eigenValues = EigenValuesFromMatrix(asgMatrix);

    const float3x3 IDENTITY = float3x3(
        float3(1.0f, 0.0f, 0.0f),
        float3(0.0f, 1.0f, 0.0f),
        float3(0.0f, 0.0f, 1.0f)
    );

    float maxValue = eigenValues.z;
    float3x3 eMat3 = asgMatrix - eigenValues.z * IDENTITY;
    float3x3 eMat1 = asgMatrix - eigenValues.x * IDENTITY;

    res.normal = EigenVectorFromMatrix(eMat3);
    res.tangent = EigenVectorFromMatrix(eMat1);
    res.bitangent = cross(res.normal, res.tangent);

    res.sharpness.x = maxValue - eigenValues.x;
    res.sharpness.y = maxValue - eigenValues.y;

    res.amplitude = exp(maxValue);

    return res;
}

AnisotropicSphericalGaussian AnisotropicSphericalGaussianFromMatrix(float3x3 asgMatrix, float eMax)
{
    AnisotropicSphericalGaussian res;

    float3 eigenValues = EigenValuesFromMatrix(asgMatrix, eMax);

    const float3x3 IDENTITY = float3x3(
        float3(1.0f, 0.0f, 0.0f),
        float3(0.0f, 1.0f, 0.0f),
        float3(0.0f, 0.0f, 1.0f)
    );

    float maxValue = eigenValues.z;
    float3x3 eMat3 = asgMatrix - eigenValues.z * IDENTITY;
    float3x3 eMat1 = asgMatrix - eigenValues.x * IDENTITY;

    res.normal = EigenVectorFromMatrix(eMat3);
    res.tangent = EigenVectorFromMatrix(eMat1);
    res.bitangent = cross(res.normal, res.tangent);

    res.sharpness.x = maxValue - eigenValues.x;
    res.sharpness.y = maxValue - eigenValues.y;

    res.amplitude = exp(maxValue);

    return res;
}

// Vector product of two anisotropic spherical gaussians.
// Due to eigen decomposition, this function is susceptible to precision problems.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
AnisotropicSphericalGaussian AnisotropicSphericalGaussianVectorProductASG(AnisotropicSphericalGaussian lhs, AnisotropicSphericalGaussian rhs)
{
    // The product of two asgs is the sum of their matrix representations.
    float3x3 productMatrix = AnisotropicSphericalGaussianToMatrix(lhs) + AnisotropicSphericalGaussianToMatrix(rhs);

    AnisotropicSphericalGaussian productAsg = AnisotropicSphericalGaussianFromMatrix(productMatrix);//, log(lhs.amplitude * rhs.amplitude));

    // Correct any sign flip that may have happened in the asg->matrix->asg conversion:
    productAsg.normal = dot(lhs.normal + rhs.normal, productAsg.normal) >= 0.0f ? productAsg.normal : -productAsg.normal;

    // Product of hemispherical clamping functions is smooth. Approximately evaluate scalar product, outside of the integral.
    productAsg.amplitude *= max(0.0f, dot(lhs.normal, productAsg.normal)) * max(0.0f, dot(rhs.normal, productAsg.normal));

    return productAsg;
}

// Inner product of two anisotropic spherical gaussians.
// TODO: Can be greatly simplified. Currently bruteforcing.
// http://cg.cs.tsinghua.edu.cn/papers/SIGASIA-2013-asg.pdf
float AnisotropicSphericalGaussianInnerProductASG(AnisotropicSphericalGaussian lhs, AnisotropicSphericalGaussian rhs)
{
    return AnisotropicSphericalGaussianIntegral(AnisotropicSphericalGaussianVectorProductASG(lhs, rhs));
}

#endif