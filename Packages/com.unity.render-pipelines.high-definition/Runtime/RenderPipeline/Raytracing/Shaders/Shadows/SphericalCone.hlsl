void SampleSphericalCone(float3 conePosition, float coneRadius, float3 coneDir,
                        float coneAngle,
                        float u, float v,
                        out float3 outPosition, out float outPDF)
{
    // Compute the half angle cosine
    float halfAngleCos = cos(coneAngle * 0.5f);

    // Compute coordinate system for sphere sampling.
    float3x3 localToWorld = GetLocalFrame(coneDir);

    // The light is a spotlight: sample in the light's cone to increase convergence.
    float cosTheta = (1.0f - u) + u * halfAngleCos;
    float sinTheta = sqrt(1.0f - cosTheta * cosTheta);
    float phi = v * 2.0f * PI;

    // Compute the local space sampling direction
    float3 smp_dir = float3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);

    // Compute the sampling direction
    float3 wsmp_dir;
    wsmp_dir = smp_dir.x * localToWorld[0];
    wsmp_dir = smp_dir.y * localToWorld[1] + wsmp_dir;
    wsmp_dir = smp_dir.z * localToWorld[2] + wsmp_dir;

    // Compute the world position of the sample
    outPosition = coneRadius * wsmp_dir + conePosition;

    // Uniform cone PDF.
    outPDF = 2.0f * PI * (1 - halfAngleCos);
}
