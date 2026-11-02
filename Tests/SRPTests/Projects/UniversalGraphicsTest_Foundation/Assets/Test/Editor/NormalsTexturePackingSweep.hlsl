#ifndef UNIVERSAL_TEST_NORMALS_TEXTURE_PACKING_SWEEP_INCLUDED
#define UNIVERSAL_TEST_NORMALS_TEXTURE_PACKING_SWEEP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PackNormalsTexture.hlsl"

float MaxNormalRoundTripErrorDegrees(int stepCount)
{
    float maxErrorDegrees = 0.0;

    for (int i = 0; i < stepCount; i++)
    {
        float cosTheta = 1.0 - 2.0 * (float(i) + 0.5) / float(stepCount);
        float sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));

        for (int j = 0; j < stepCount; j++)
        {
            float phi = 2.0 * PI * (float(j) + 0.5) / float(stepCount);
            float3 normalWS = normalize(float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta));

            float3 roundTripped = normalize(UnpackNormalWSFromTexture(PackNormalWSToTexture(normalWS)));

            float cosError = clamp(dot(normalWS, roundTripped), -1.0, 1.0);
            maxErrorDegrees = max(maxErrorDegrees, degrees(acos(cosError)));
        }
    }

    return maxErrorDegrees;
}

float MaxAxisAlignedRoundTripErrorDegrees()
{
    float3 axes[6] =
    {
        float3(1.0, 0.0, 0.0), float3(-1.0, 0.0, 0.0),
        float3(0.0, 1.0, 0.0), float3(0.0, -1.0, 0.0),
        float3(0.0, 0.0, 1.0), float3(0.0, 0.0, -1.0)
    };

    float maxErrorDegrees = 0.0;

    for (int i = 0; i < 6; i++)
    {
        float3 roundTripped = normalize(UnpackNormalWSFromTexture(PackNormalWSToTexture(axes[i])));

        float cosError = clamp(dot(axes[i], roundTripped), -1.0, 1.0);
        maxErrorDegrees = max(maxErrorDegrees, degrees(acos(cosError)));
    }

    return maxErrorDegrees;
}

#endif
