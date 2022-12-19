void SampleSphericalPyramid(float3 conePosition, float coneRadius,
                        float3 coneDir, float3 coneRight, float3 coneUp,
                        float coneAngleX, float coneAngleY,
                        float u, float v,
                        out float3 outPosition, out float outPDF)
{
    // The light is a spotlight: sample in the light's pyramid cone to increase convergence.
    float thetaX = coneAngleX * (u - 0.5f);
    float thetaY = coneAngleY * (v - 0.5f);

    // Compute the local space sampling direction
    float cosThetaY = cos(thetaY);
    float3 smp_dir = float3(cos(thetaX) * cosThetaY, sin(thetaX) * cosThetaY, sin(thetaY));

    // Compute the sampling direction
    float3 wsmp_dir;
    wsmp_dir = smp_dir.x * coneDir;
    wsmp_dir = smp_dir.y * coneRight + wsmp_dir;
    wsmp_dir = smp_dir.z * coneUp + wsmp_dir;

    // Compute the world position of the sample
    outPosition = coneRadius * wsmp_dir + conePosition;

    // Product of two separable PDFs over each pyramid cone angle.
    outPDF = PI * PI / (coneAngleX * coneAngleY);
}
