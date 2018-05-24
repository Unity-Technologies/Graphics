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

        // For our isotropic area lights which expect radiance(W / (sr* m^2)) in the shader:
        // power = Integral{area, Integral{hemisphere, radiance * <N, L>}},
        // power = area * Pi * radiance,
        // radiance = power / (area * Pi).
        // We use photometric unit, so radiance is luminance and power is luminous power

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
            //Line lights expect radiance (W / (sr * m^2)) in the shader.
            //In the UI, we specify luminous flux (power) in lumens.
            //First, it needs to be converted to radiometric units (radiant flux, W).

            //Then we must recall how to compute power from radiance:

            //radiance = differential_power / (differrential_projected_area * differential_solid_angle),
            //radiance = differential_power / (differrential_area * differential_solid_angle * <N, L>),
            //power = Integral{area, Integral{hemisphere, radiance * <N, L>}}.

            //Unlike tube lights, our line lights have no surface area, so the integral becomes:

            //power = Integral{length, Integral{sphere, radiance}}.

            //For an isotropic line light, radiance is constant, therefore:

            //power = length * (4 * Pi) * radiance,
            //radiance = power / (length * (4 * Pi)).
            return intensity / (4.0f * Mathf.PI * lineWidth);
        }

        public static void CalculateAnglesForPyramid(float aspectRatio, float spotAngle, out float angleA, out float angleB)
        {
            // Since the smallest angles is = to the fov, and we don't care of the angle order, simply make sure the aspect ratio is > 1
            if (aspectRatio < 1.0f)
                aspectRatio = 1.0f / aspectRatio;

            angleA = spotAngle * Mathf.Deg2Rad;

            var halfAngle = angleA * 0.5f; // half of the smallest angle
            var length = Mathf.Tan(halfAngle); // half length of the smallest side of the rectangle
            length *= aspectRatio; // half length of the bigest side of the rectangle
            halfAngle = Mathf.Atan(length); // half of the bigest angle

            angleB = halfAngle * 2.0f;
        }
    }
}
