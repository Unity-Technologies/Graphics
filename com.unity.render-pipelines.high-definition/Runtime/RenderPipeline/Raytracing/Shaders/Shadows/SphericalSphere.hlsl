float dist2(float3 vecA, float3 vecB)
{
	float3 res = vecA - vecB;
	return dot(res, res);
}

float dist(float3 vecA, float3 vecB)
{
	float3 res = vecA - vecB;
	return length(res);
}

void SampleSphericalSphere(float3 spherePosition, float sphereRadius, float u, float v, float3 position, float3 normal, out float3 outPosition, out float outPDF)
{
    // Compute coordinate system for sphere sampling.
    float3 wc = normalize(spherePosition - position);
    float3x3 localToWorld = GetLocalFrame(wc);

    // Sample sphere uniformly inside subtended cone.
    float sphereRadius2 = sphereRadius * sphereRadius;
    float sinThetaMax2 = sphereRadius2 / dist2(position, spherePosition);
    float cosThetaMax = sqrt(max(0.0f, 1.0f - sinThetaMax2));
    float cosTheta = (1.0f - u) + u * cosThetaMax;
    float sinTheta = sqrt(max(0.0f, 1.0f - cosTheta * cosTheta));
    float phi = v * 2.0f * PI;

    float dc = dist(position, spherePosition);
    float ds = dc * cosTheta - sqrt(max(0.0f, sphereRadius2 - dc * dc * sinTheta * sinTheta));
    float cosAlpha = (dc * dc + sphereRadius2 - ds * ds) / (2.0f * dc * sphereRadius);
    float sinAlpha = sqrt(max(0.0f, 1.0f - cosAlpha * cosAlpha));

    // Compute surface normal and sampled point on sphere.
    float cphi = cos(phi);
    float sphi = sin(phi);
  	float3 nWorld =  sinAlpha * cphi * -localToWorld[0] + sinAlpha * sphi * -localToWorld[1] + cosAlpha * -wc;

    // Compute the world position of the sample
    outPosition = sphereRadius * nWorld + spherePosition;

    // Uniform cone PDF.
    outPDF = 2.0f * PI * (1 - cosThetaMax);
}
