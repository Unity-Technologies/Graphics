using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Light Utils contains function to convert light intensities between units
    /// </summary>
    class LightUtils
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

        /// <summary>
        /// Convert an intensity in Lumen to Candela for a point light
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public static float ConvertPointLightLumenToCandela(float intensity)
            => intensity / (4.0f * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a point light
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        public static float ConvertPointLightCandelaToLumen(float intensity)
            => intensity * (4.0f * Mathf.PI);

        // angle is the full angle, not the half angle in radian
        // convert intensity (lumen) to candela
        /// <summary>
        /// Convert an intensity in Lumen to Candela for a cone spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angle">Full angle in radian</param>
        /// <param name="exact">Exact computation or an approximation</param>
        /// <returns></returns>
        public static float ConvertSpotLightLumenToCandela(float intensity, float angle, bool exact)
            => exact ? intensity / (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity / Mathf.PI;

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a cone pot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angle">Full angle in radian</param>
        /// <param name="exact">Exact computation or an approximation</param>
        /// <returns></returns>
        public static float ConvertSpotLightCandelaToLumen(float intensity, float angle, bool exact)
            => exact ? intensity * (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity * Mathf.PI;

        // angleA and angleB are the full opening angle, not half angle
        // convert intensity (lumen) to candela
        /// <summary>
        /// Convert an intensity in Lumen to Candela for a pyramid spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angleA">Full opening angle in radian</param>
        /// <param name="angleB">Full opening angle in radian</param>
        /// <returns></returns>
        public static float ConvertFrustrumLightLumenToCandela(float intensity, float angleA, float angleB)
            => intensity / (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));

        /// <summary>
        /// Convert an intensity in Candela to Lumen for a pyramid spot light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="angleA">Full opening angle in radian</param>
        /// <param name="angleB">Full opening angle in radian</param>
        /// <returns></returns>
        public static float ConvertFrustrumLightCandelaToLumen(float intensity, float angleA, float angleB)
            => intensity * (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a sphere light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        public static float ConvertSphereLightLumenToLuminance(float intensity, float sphereRadius)
            => intensity / ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a sphere light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        public static float ConvertSphereLightLuminanceToLumen(float intensity, float sphereRadius)
            => intensity * ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a disc light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="discRadius"></param>
        /// <returns></returns>
        public static float ConvertDiscLightLumenToLuminance(float intensity, float discRadius)
            => intensity / ((discRadius * discRadius * Mathf.PI) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a disc light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="discRadius"></param>
        /// <returns></returns>
        public static float ConvertDiscLightLuminanceToLumen(float intensity, float discRadius)
            => intensity * ((discRadius * discRadius * Mathf.PI) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Lumen to Luminance(nits) for a rectangular light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertRectLightLumenToLuminance(float intensity, float width, float height)
            => intensity / ((width * height) * Mathf.PI);

        /// <summary>
        /// Convert an intensity in Luminance(nits) to Lumen for a rectangular light.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertRectLightLuminanceToLumen(float intensity, float width, float height)
            => intensity * ((width * height) * Mathf.PI);

        // Helper for Lux, Candela, Luminance, Ev conversion
        /// <summary>
        /// Convert intensity in Lux at a certain distance in Candela.
        /// </summary>
        /// <param name="lux"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertLuxToCandela(float lux, float distance)
            => lux * distance * distance;

        /// <summary>
        /// Convert intensity in Candela at a certain distance in Lux.
        /// </summary>
        /// <param name="candela"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertCandelaToLux(float candela, float distance)
            => candela / (distance * distance);

        /// <summary>
        /// Convert EV100 to Luminance(nits)
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public static float ConvertEvToLuminance(float ev)
        {
            float k = ColorUtils.s_LightMeterCalibrationConstant;
            return (k / 100.0f) * Mathf.Pow(2, ev);
        }
            

        /// <summary>
        /// Convert EV100 to Candela
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public static float ConvertEvToCandela(float ev)
            // From punctual point of view candela and luminance is the same
            => ConvertEvToLuminance(ev);

        /// <summary>
        /// Convert EV100 to Lux at a certain distance
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertEvToLux(float ev, float distance)
            // From punctual point of view candela and luminance is the same
            => ConvertCandelaToLux(ConvertEvToLuminance(ev), distance);

        /// <summary>
        /// Convert Luminance(nits) to EV100
        /// </summary>
        /// <param name="luminance"></param>
        /// <returns></returns>
        public static float ConvertLuminanceToEv(float luminance)
        {
            float k = ColorUtils.s_LightMeterCalibrationConstant;
            return (float)Math.Log((luminance * 100f) / k, 2);
        }

        /// <summary>
        /// Convert Candela to EV100
        /// </summary>
        /// <param name="candela"></param>
        /// <returns></returns>
        public static float ConvertCandelaToEv(float candela)
            // From punctual point of view candela and luminance is the same
            => ConvertLuminanceToEv(candela);

        /// <summary>
        /// Convert Lux at a certain distance to EV100
        /// </summary>
        /// <param name="lux"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertLuxToEv(float lux, float distance)
            // From punctual point of view candela and luminance is the same
            => ConvertLuminanceToEv(ConvertLuxToCandela(lux, distance));

        // Helper for punctual and area light unit conversion
        /// <summary>
        /// Convert a punctual light intensity in Lumen to Candela
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToCandela(HDLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
        {
            if (lightType == HDLightType.Spot && enableSpotReflector)
            {
                // We have already calculate the correct value, just assign it
                return initialIntensity;
            }
            return ConvertPointLightLumenToCandela(lumen);
        }

        /// <summary>
        /// Convert a punctual light intensity in Lumen to Lux
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToLux(HDLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector, float distance)
        {
            float candela = ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector);
            return ConvertCandelaToLux(candela, distance);
        }

        /// <summary>
        /// Convert a punctual light intensity in Candela to Lumen
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="spotLightShape"></param>
        /// <param name="candela"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <param name="aspectRatio"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightCandelaToLumen(HDLightType lightType, SpotLightShape spotLightShape, float candela, bool enableSpotReflector, float spotAngle, float aspectRatio)
        {
            if (lightType == HDLightType.Spot && enableSpotReflector)
            {
                // We just need to multiply candela by solid angle in this case
                if (spotLightShape == SpotLightShape.Cone)
                    return ConvertSpotLightCandelaToLumen(candela, spotAngle * Mathf.Deg2Rad, true);
                else if (spotLightShape == SpotLightShape.Pyramid)
                {
                    float angleA, angleB;
                    CalculateAnglesForPyramid(aspectRatio, spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                    return ConvertFrustrumLightCandelaToLumen(candela, angleA, angleB);
                }
                else // Box
                    return ConvertPointLightCandelaToLumen(candela);
            }
            return ConvertPointLightCandelaToLumen(candela);
        }

        /// <summary>
        /// Convert a punctual light intensity in Lux to Lumen
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="spotLightShape"></param>
        /// <param name="lux"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLuxToLumen(HDLightType lightType, SpotLightShape spotLightShape, float lux, bool enableSpotReflector, float spotAngle, float aspectRatio, float distance)
        {
            float candela = ConvertLuxToCandela(lux, distance);
            return ConvertPunctualLightCandelaToLumen(lightType, spotLightShape, candela, enableSpotReflector, spotAngle, aspectRatio);
        }

        // This is not correct, we use candela instead of luminance but this is request from artists to support EV100 on punctual light
        /// <summary>
        /// Convert a punctual light intensity in EV100 to Lumen.
        /// This is not physically correct but it's handy to have EV100 for punctual lights.
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="spotLightShape"></param>
        /// <param name="ev"></param>
        /// <param name="enableSpotReflector"></param>
        /// <param name="spotAngle"></param>
        /// <param name="aspectRatio"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightEvToLumen(HDLightType lightType, SpotLightShape spotLightShape, float ev, bool enableSpotReflector, float spotAngle, float aspectRatio)
        {
            float candela = ConvertEvToCandela(ev);
            return ConvertPunctualLightCandelaToLumen(lightType, spotLightShape, candela, enableSpotReflector, spotAngle, aspectRatio);
        }

        // This is not correct, we use candela instead of luminance but this is request from artists to support EV100 on punctual light
        /// <summary>
        /// Convert a punctual light intensity in Lumen to EV100.
        /// This is not physically correct but it's handy to have EV100 for punctual lights.
        /// </summary>
        /// <param name="lightType"></param>
        /// <param name="lumen"></param>
        /// <param name="initialIntensity"></param>
        /// <param name="enableSpotReflector"></param>
        /// <returns></returns>
        public static float ConvertPunctualLightLumenToEv(HDLightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
        {
            float candela = ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector);
            return ConvertCandelaToEv(candela);
        }

        /// <summary>
        /// Convert area light intensity in Lumen to Luminance(nits)
        /// </summary>
        /// <param name="areaLightShape"></param>
        /// <param name="lumen"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertAreaLightLumenToLuminance(AreaLightShape areaLightShape, float lumen, float width, float height = 0)
        {
            switch (areaLightShape)
            {
                case AreaLightShape.Tube:
                    return LightUtils.CalculateLineLightLumenToLuminance(lumen, width);
                case AreaLightShape.Rectangle:
                    return LightUtils.ConvertRectLightLumenToLuminance(lumen, width, height);
                case AreaLightShape.Disc:
                    return LightUtils.ConvertDiscLightLumenToLuminance(lumen, width);
            }
            return lumen;
        }

        /// <summary>
        /// Convert area light intensity in Luminance(nits) to Lumen
        /// </summary>
        /// <param name="areaLightShape"></param>
        /// <param name="luminance"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertAreaLightLuminanceToLumen(AreaLightShape areaLightShape, float luminance, float width, float height = 0)
        {
            switch (areaLightShape)
            {
                case AreaLightShape.Tube:
                    return LightUtils.CalculateLineLightLuminanceToLumen(luminance, width);
                case AreaLightShape.Rectangle:
                    return LightUtils.ConvertRectLightLuminanceToLumen(luminance, width, height);
                case AreaLightShape.Disc:
                    return LightUtils.ConvertDiscLightLuminanceToLumen(luminance, width);
            }
            return luminance;
        }

        /// <summary>
        /// Convert area light intensity in Lumen to EV100
        /// </summary>
        /// <param name="AreaLightShape"></param>
        /// <param name="lumen"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertAreaLightLumenToEv(AreaLightShape AreaLightShape, float lumen, float width, float height)
        {
            float luminance = ConvertAreaLightLumenToLuminance(AreaLightShape, lumen, width, height);
            return ConvertLuminanceToEv(luminance);
        }

        /// <summary>
        /// Convert area light intensity in EV100 to Lumen
        /// </summary>
        /// <param name="AreaLightShape"></param>
        /// <param name="ev"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static float ConvertAreaLightEvToLumen(AreaLightShape AreaLightShape, float ev, float width, float height)
        {
            float luminance = ConvertEvToLuminance(ev);
            return ConvertAreaLightLuminanceToLumen(AreaLightShape, luminance, width, height);
        }

        /// <summary>
        /// Convert line light intensity in Lumen to Luminance(nits)
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="lineWidth"></param>
        /// <returns></returns>
        public static float CalculateLineLightLumenToLuminance(float intensity, float lineWidth)
        {
            //Line lights expect radiance (W / (sr * m^2)) in the shader.
            //In the UI, we specify luminous flux (power) in lumens.
            //First, it needs to be converted to radiometric units (radian flux, W).

            //Then we must recall how to compute power from radiance:

            //radiance = differential_power / (differential_projected_area * differential_solid_angle),
            //radiance = differential_power / (differential_area * differential_solid_angle * <N, L>),
            //power = Integral{area, Integral{hemisphere, radiance * <N, L>}}.

            //Unlike line lights, our line lights have no surface area, so the integral becomes:

            //power = Integral{length, Integral{sphere, radiance}}.

            //For an isotropic line light, radiance is constant, therefore:

            //power = length * (4 * Pi) * radiance,
            //radiance = power / (length * (4 * Pi)).
            return intensity / (4.0f * Mathf.PI * lineWidth);
        }

        /// <summary>
        /// Convert a line light intensity in Luminance(nits) to Lumen
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="lineWidth"></param>
        /// <returns></returns>
        public static float CalculateLineLightLuminanceToLumen(float intensity, float lineWidth)
            => intensity * (4.0f * Mathf.PI * lineWidth);

        // spotAngle in radian
        /// <summary>
        /// Calculate angles for the pyramid spot light to calculate it's intensity.
        /// </summary>
        /// <param name="aspectRatio"></param>
        /// <param name="spotAngle">angle in radian</param>
        /// <param name="angleA"></param>
        /// <param name="angleB"></param>
        public static void CalculateAnglesForPyramid(float aspectRatio, float spotAngle, out float angleA, out float angleB)
        {
            // Since the smallest angles is = to the fov, and we don't care of the angle order, simply make sure the aspect ratio is > 1
            if (aspectRatio < 1.0f)
                aspectRatio = 1.0f / aspectRatio;

            angleA = spotAngle;

            var halfAngle = angleA * 0.5f; // half of the smallest angle
            var length = Mathf.Tan(halfAngle); // half length of the smallest side of the rectangle
            length *= aspectRatio; // half length of the bigest side of the rectangle
            halfAngle = Mathf.Atan(length); // half of the bigest angle

            angleB = halfAngle * 2.0f;
        }

        internal static void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, HDAdditionalLightData hdLight, Light light)
        {
            float intensity = hdLight.intensity;
            float luxAtDistance = hdLight.luxAtDistance;
            HDLightType lightType = hdLight.ComputeLightType(light);

            // For punctual lights
            if (lightType != HDLightType.Area)
            {
                // Lumen ->
                if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertPunctualLightLumenToCandela(lightType, intensity, light.intensity, hdLight.enableSpotReflector);
                else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertPunctualLightLumenToLux(lightType, intensity, light.intensity, hdLight.enableSpotReflector, hdLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertPunctualLightLumenToEv(lightType, intensity, light.intensity, hdLight.enableSpotReflector);
                // Candela ->
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightCandelaToLumen(lightType, hdLight.spotLightShape, intensity, hdLight.enableSpotReflector, light.spotAngle, hdLight.aspectRatio);
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertCandelaToLux(intensity, hdLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertCandelaToEv(intensity);
                // Lux ->
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightLuxToLumen(lightType, hdLight.spotLightShape, intensity, hdLight.enableSpotReflector,
                                                                          light.spotAngle, hdLight.aspectRatio, hdLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertLuxToCandela(intensity, hdLight.luxAtDistance);
                else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertLuxToEv(intensity, hdLight.luxAtDistance);
                // EV100 ->
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertPunctualLightEvToLumen(lightType, hdLight.spotLightShape, intensity, hdLight.enableSpotReflector,
                                                                         light.spotAngle, hdLight.aspectRatio);
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Candela)
                    intensity = LightUtils.ConvertEvToCandela(intensity);
                else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lux)
                    intensity = LightUtils.ConvertEvToLux(intensity, hdLight.luxAtDistance);
            }
            else  // For area lights
            {
                if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Nits)
                    intensity = LightUtils.ConvertAreaLightLumenToLuminance(hdLight.areaLightShape, intensity, hdLight.shapeWidth, hdLight.shapeHeight);
                if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertAreaLightLuminanceToLumen(hdLight.areaLightShape, intensity, hdLight.shapeWidth, hdLight.shapeHeight);
                if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertLuminanceToEv(intensity);
                if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Nits)
                    intensity = LightUtils.ConvertEvToLuminance(intensity);
                if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                    intensity = LightUtils.ConvertAreaLightEvToLumen(hdLight.areaLightShape, intensity, hdLight.shapeWidth, hdLight.shapeHeight);
                if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                    intensity = LightUtils.ConvertAreaLightLumenToEv(hdLight.areaLightShape, intensity, hdLight.shapeWidth, hdLight.shapeHeight);
            }

            hdLight.intensity = intensity;
        }
    }
}
