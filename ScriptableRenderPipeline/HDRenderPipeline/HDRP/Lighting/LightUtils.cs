using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LightUtils
    {
        // Physical light unit helper
        // All light unit are in lumen (Luminous power)
        // Punctual light (point, spot) are convert to candela (cd = lumens / steradian)
        // Area light are convert to luminance (cd/(m^2*steradian)) with the following formulation: Luminous Power / (Area * PI * steradian)

        // Ref: Moving Frostbite to PBR
        // Also good ref: https://www.radiance-online.org/community/workshops/2004-fribourg/presentations/Wandachowicz_paper.pdf

        // convert intensity (lumen) to candela
        public static float ConvertPointLightIntensity(float intensity)
        {
            return intensity / (4.0f * Mathf.PI);
        }

        // angle is the full angle, not the half angle in radiant
        // convert intensity (lumen) to candela
        public static float ConvertSpotLightIntensity(float intensity, float angle, bool exact)
        {
            return exact ? intensity / (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity / Mathf.PI;
        }

        // angleA and angleB are the full opening angle, not half angle
        // convert intensity (lumen) to candela
        public static float ConvertFrustrumLightIntensity(float intensity, float angleA, float angleB)
        {
            return intensity / (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));
        }

        // convert intensity (lumen) to nits
        public static float ConvertSphereLightIntensity(float intensity, float sphereRadius)
        {
            return intensity / ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);
        }

        // convert intensity (lumen) to nits
        public static float ConvertDiscLightIntensity(float intensity, float discRadius)
        {
            return intensity / ((discRadius * discRadius * Mathf.PI) * Mathf.PI);
        }

        // convert intensity (lumen) to nits
        public static float ConvertRectLightIntensity(float intensity, float width, float height)
        {
            return intensity / ((width * height) * Mathf.PI);
        }

        // convert intensity (lumen) to nits
        public static float CalculateLineLightIntensity(float intensity, float lineWidth)
        {
            // Line lights in the shader expect intensity (W / sr).
            // In the UI, we specify luminous flux(power) in lumens.
            // First, it needs to be converted to radiometric units(radiant flux, W).
            // Then we must recall how to compute power from intensity for a line light:
            // power = Integral{length, Integral{sphere, intensity}}.
            // For an isotropic line light, intensity is constant, so
            // power = length * (4 * Pi) * intensity,
            // intensity = power / (length * (4 * Pi)).
            return intensity / (4.0f * Mathf.PI * lineWidth);
        }
    }
}
