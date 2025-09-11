using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.PathTracing.Core
{
    internal struct LightFalloffDesc
    {
        public float LUTRange;
        public Experimental.GlobalIllumination.FalloffType FalloffType;
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(LUTRange, FalloffType);
        }
    }

    internal class LightFalloffLUT
    {
        // Inverse squared falloff: minimum distance to light to avoid division by zero
        public const float DistThresholdSqr = 0.0001f; // 1cm (in Unity 1 is 1m) so this is 0.01^2

        // Legacy Unity falloff: where the falloff down to zero should start
        private const float ToZeroFadeStart = 0.8f * 0.8f;

        // Legacy Unity falloff: constants for OpenGL attenuation
        private const float ConstantFac = 1.000f;
        private const float QuadraticFac = 25.0f;

        // Calculate the quadratic attenuation factor for a light with a specified range
        private static float CalculateLightQuadFac(float range)
        {
            return QuadraticFac / (range * range);
        }

        private static float LightAttenuateNormalized(float distSqr)
        {
            // match the vertex lighting falloff
            float atten = 1 / (ConstantFac + CalculateLightQuadFac(1.0f) * distSqr);

            // ...but vertex one does not falloff to zero at light's range;
            // So force it to falloff to zero at the edges.
            if (distSqr >= ToZeroFadeStart)
            {
                if (distSqr > 1)
                    atten = 0;
                else
                    atten *= 1 - (distSqr - ToZeroFadeStart) / (1 - ToZeroFadeStart);
            }

            return atten;
        }

        public static float LegacyUnityFalloff(float normalizedDistance)
        {
            float clampedDist = math.clamp(normalizedDistance, 0.0f, 1.0f);
            return LightAttenuateNormalized(clampedDist * clampedDist);
        }

        public static float SmoothDistanceAttenuation(float squaredDistance, float invSqrAttenuationRadius)
        {
            float factor = squaredDistance * invSqrAttenuationRadius;
            float smoothFactor = math.saturate(1.0f - factor * factor);
            return smoothFactor * smoothFactor;
        }

        public static float InverseSquaredFalloffSmooth(float squaredDistance, float invSqrAttenuationRadius)
        {
            float attenuation = 1.0f / (math.max(DistThresholdSqr, squaredDistance));
            // Non physically based hack to limit light influence to attenuationRadius. As we approach the range we fade out the light.
            return attenuation * SmoothDistanceAttenuation(squaredDistance, invSqrAttenuationRadius);
        }

        public static float InverseSquaredFalloff(float squaredDistance)
        {
            return 1.0f / (math.max(DistThresholdSqr, squaredDistance));
        }

        public static float[] BuildLightFalloffLUTs(LightFalloffDesc[] lightFalloffDescs, uint lightFalloffLUTLength = 1024)
        {
            List<float> lightFalloffData = new();
            foreach (var lightFalloffDesc in lightFalloffDescs)
            {
                float range = lightFalloffDesc.LUTRange;
                switch (lightFalloffDesc.FalloffType)
                {
                    case Experimental.GlobalIllumination.FalloffType.InverseSquaredNoRangeAttenuation:
                        {
                            for (uint k = 0; k < lightFalloffLUTLength; ++k)
                            {
                                float normalizedTableDistance = (float)k / (float)(lightFalloffLUTLength - 1);
                                float distance = range * normalizedTableDistance;
                                float value = InverseSquaredFalloff(distance * distance);
                                lightFalloffData.Add(value);
                            }
                        }
                        break;
                    case Experimental.GlobalIllumination.FalloffType.InverseSquared:
                        {
                            float invSqrAttenuationRadius = 1.0f / math.max(DistThresholdSqr, range * range);
                            for (uint k = 0; k < lightFalloffLUTLength; ++k)
                            {
                                float normalizedTableDistance = (float)k / (float)(lightFalloffLUTLength - 1);
                                float distance = range * normalizedTableDistance;
                                float value = InverseSquaredFalloffSmooth(distance * distance, invSqrAttenuationRadius);
                                lightFalloffData.Add(value);
                            }
                        }
                        break;
                    case Experimental.GlobalIllumination.FalloffType.Linear:
                        {
                            for (uint k = 0; k < lightFalloffLUTLength; ++k)
                            {
                                float linear = 1.0f - ((float)k / (float)(lightFalloffLUTLength - 1));
                                lightFalloffData.Add(linear);
                            }
                        }
                        break;
                    case Experimental.GlobalIllumination.FalloffType.Legacy:
                    default:
                        {
                            for (uint k = 0; k < lightFalloffLUTLength; ++k)
                            {
                                float normalizedTableDistance = (float)k / (float)(lightFalloffLUTLength - 1);
                                float value = LegacyUnityFalloff(normalizedTableDistance);
                                lightFalloffData.Add(value);
                            }
                        }
                        break;
                }
                // Change the last value to 0 to limit the light influence to the range
                lightFalloffData[^1] = 0.0f;
            }
            return lightFalloffData.ToArray();
        }
    }
}
