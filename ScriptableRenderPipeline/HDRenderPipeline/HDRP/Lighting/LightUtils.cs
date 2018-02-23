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
        public static float calculateLineLightArea(float intensity, float lineWidth)
        {
            // The area of a cylinder is this:
            // float lineRadius = 0.01f; // 1cm
            //return intensity / (2.0f * Mathf.PI * lineRadius * lineWidth * Mathf.PI);
            // But with our current line light algorithm we get an insane gap in intensity
            // following formula (fully empirical) give a better match to a rect light of 1cm of width.
            // It is basically point light intensity / line width.
            return intensity / (4.0f * Mathf.PI * lineWidth);
        }
    }
}
