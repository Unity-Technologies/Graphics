using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Light Unit Utils contains functions and definitions to facilitate conversion between different light intensity units.
    /// </summary>
    public static class LightUnitUtils
    {
        static float k_LuminanceToEvFactor => Mathf.Log(100f / ColorUtils.s_LightMeterCalibrationConstant, 2);

        static float k_EvToLuminanceFactor => -k_LuminanceToEvFactor;

        /// <summary>
        /// The solid angle of a full sphere in steradians.
        /// </summary>
        public const float SphereSolidAngle = 4.0f * Mathf.PI;

        /// <summary>
        /// Get the unit that light intensity is measured in, for a specific light type.
        /// </summary>
        /// <param name="lightType">The type of light to get the native light unit for.</param>
        /// <returns>The native unit of that light types intensity.</returns>
        public static LightUnit GetNativeLightUnit(LightType lightType)
        {
            switch (lightType)
            {
                // Punctual lights
                case LightType.Spot:
                case LightType.Point:
                case LightType.Pyramid:
                    return LightUnit.Candela;

                // Directional lights
                case LightType.Directional:
                case LightType.Box:
                    return LightUnit.Lux;

                // Area lights
                case LightType.Rectangle:
                case LightType.Disc:
                case LightType.Tube:
                    return LightUnit.Nits;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Check if a light types intensity can be converted to/from a light unit.
        /// </summary>
        /// <param name="lightType">Light type to check.</param>
        /// <param name="lightUnit">Unit to check.</param>
        /// <returns>True if light unit is supported.</returns>
        public static bool IsLightUnitSupported(LightType lightType, LightUnit lightUnit)
        {
            const int punctualUnits = 1 << (int)LightUnit.Lumen |
                                      1 << (int)LightUnit.Candela |
                                      1 << (int)LightUnit.Lux |
                                      1 << (int)LightUnit.Ev100;

            const int directionalUnits = 1 << (int)LightUnit.Lux;

            const int areaUnits = 1 << (int)LightUnit.Lumen |
                                  1 << (int)LightUnit.Nits |
                                  1 << (int)LightUnit.Ev100;

            int lightUnitFlag = 1 << (int)lightUnit;

            switch (lightType)
            {
                // Punctual lights
                case LightType.Point:
                case LightType.Spot:
                case LightType.Pyramid:
                    return (lightUnitFlag & punctualUnits) > 0;

                // Directional lights
                case LightType.Directional:
                case LightType.Box:
                    return (lightUnitFlag & directionalUnits) > 0;

                // Area lights
                case LightType.Rectangle:
                case LightType.Disc:
                case LightType.Tube:
                    return (lightUnitFlag & areaUnits) > 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the solid angle of a Point light.
        /// </summary>
        /// <returns>4 * Pi steradians.</returns>
        public static float GetSolidAngleFromPointLight()
        {
            return SphereSolidAngle;
        }

        /// <summary>
        /// Get the solid angle of a Spot light.
        /// </summary>
        /// <param name="spotAngle">The spot angle in degrees.</param>
        /// <returns>Solid angle in steradians.</returns>
        public static float GetSolidAngleFromSpotLight(float spotAngle)
        {
            double angle = Math.PI * spotAngle / 180.0;
            double solidAngle = 2.0 * Math.PI * (1.0 - Math.Cos(angle * 0.5));
            return (float)solidAngle;
        }

        /// <summary>
        /// Get the solid angle of a Pyramid light.
        /// </summary>
        /// <param name="spotAngle">The spot angle in degrees.</param>
        /// <param name="aspectRatio">The aspect ratio of the pyramid.</param>
        /// <returns>Solid angle in steradians.</returns>
        public static float GetSolidAngleFromPyramidLight(float spotAngle, float aspectRatio)
        {
            if (aspectRatio < 1.0f)
            {
                aspectRatio = (float)(1.0 / aspectRatio);
            }

            double angleA = Math.PI * spotAngle / 180.0;
            double length = Math.Tan(0.5 * angleA) * aspectRatio;
            double angleB = Math.Atan(length) * 2.0;
            double solidAngle = 4.0 * Math.Asin(Math.Sin(angleA * 0.5) * Math.Sin(angleB * 0.5));
            return (float)solidAngle;
        }

        internal static float GetSolidAngle(LightType lightType, bool spotReflector, float spotAngle, float aspectRatio)
        {
            return lightType switch
            {
                LightType.Spot => spotReflector ? GetSolidAngleFromSpotLight(spotAngle) : SphereSolidAngle,
                LightType.Pyramid => spotReflector ? GetSolidAngleFromPyramidLight(spotAngle, aspectRatio) : SphereSolidAngle,
                LightType.Point => GetSolidAngleFromPointLight(),
                _ => throw new ArgumentException("Solid angle is undefined for lights of type " + lightType)
            };
        }

        /// <summary>
        /// Get the projected surface area of a Rectangle light.
        /// </summary>
        /// <param name="rectSizeX">The width of the rectangle.</param>
        /// <param name="rectSizeY">The height of the rectangle.</param>
        /// <returns>Surface area.</returns>
        public static float GetAreaFromRectangleLight(float rectSizeX, float rectSizeY)
        {
            return Mathf.Abs(rectSizeX * rectSizeY) * Mathf.PI;
        }

        /// <summary>
        /// Get the projected surface area of a Rectangle light.
        /// </summary>
        /// <param name="rectSize">The size of the rectangle.</param>
        /// <returns>Projected surface area.</returns>
        public static float GetAreaFromRectangleLight(Vector2 rectSize)
        {
            return GetAreaFromRectangleLight(rectSize.x, rectSize.y);
        }

        /// <summary>
        /// Get the projected surface area of a Disc light.
        /// </summary>
        /// <param name="discRadius">The radius of the disc.</param>
        /// <returns>Projected surface area.</returns>
        public static float GetAreaFromDiscLight(float discRadius)
        {
            return discRadius * discRadius * Mathf.PI;
        }

        /// <summary>
        /// Get the projected surface area of a Tube light.
        /// </summary>
        /// <remarks>Note that Tube lights have no physical surface area.
        /// Instead this method returns a value suitable for Nits&lt;=&gt;Lumen unit conversion.</remarks>
        /// <param name="tubeLength">The length of the tube.</param>
        /// <returns>4 * Pi * (tube length).</returns>
        public static float GetAreaFromTubeLight(float tubeLength)
        {
            // Line lights expect radiance (W / (sr * m^2)) in the shader.
            // In the UI, we specify luminous flux (power) in lumens.
            // First, it needs to be converted to radiometric units (radian flux, W).
            //
            // Then we must recall how to compute power from radiance:
            //
            // radiance = differential_power / (differential_projected_area * differential_solid_angle),
            // radiance = differential_power / (differential_area * differential_solid_angle * <N, L>),
            // power = Integral{area, Integral{hemisphere, radiance * <N, L>}}.
            //
            // Unlike line lights, our line lights have no surface area, so the integral becomes:
            //
            // power = Integral{length, Integral{sphere, radiance}}.
            //
            // For an isotropic line light, radiance is constant, therefore:
            //
            // power = length * (4 * Pi) * radiance,
            // radiance = power / (length * (4 * Pi)).

            return Mathf.Abs(tubeLength) * 4.0f * Mathf.PI;
        }

        /// <summary>
        /// Convert intensity in Lumen to Candela.
        /// </summary>
        /// <param name="lumen">Intensity in Lumen.</param>
        /// <param name="solidAngle">Light solid angle in steradians.</param>
        /// <returns>Intensity in Candela.</returns>
        public static float LumenToCandela(float lumen, float solidAngle)
        {
            return lumen / solidAngle;
        }

        /// <summary>
        /// Convert intensity in Candela to Lumen.
        /// </summary>
        /// <param name="candela">Intensity in Candela.</param>
        /// <param name="solidAngle">Light solid angle in steradians.</param>
        /// <returns>Intensity in Lumen.</returns>
        public static float CandelaToLumen(float candela, float solidAngle)
        {
            return candela * solidAngle;
        }

        /// <summary>
        /// Convert intensity in Lumen to Nits.
        /// </summary>
        /// <param name="lumen">Intensity in Lumen.</param>
        /// <param name="area">Projected surface area of the light source.</param>
        /// <returns>Intensity in Nits.</returns>
        public static float LumenToNits(float lumen, float area)
        {
            return lumen / area;
        }

        /// <summary>
        /// Convert intensity in Nits to Lumen.
        /// </summary>
        /// <param name="nits">Intensity in Nits.</param>
        /// <param name="area">Projected surface area of the light source.</param>
        /// <returns>Intensity in Lumen.</returns>
        public static float NitsToLumen(float nits, float area)
        {
            return nits * area;
        }

        /// <summary>
        /// Convert intensity in Lux to Candela.
        /// </summary>
        /// <param name="lux">Intensity in Lux.</param>
        /// <param name="distance">Distance between light and surface.</param>
        /// <returns>Intensity in Candela.</returns>
        public static float LuxToCandela(float lux, float distance)
        {
            return lux / (distance * distance);
        }

        /// <summary>
        /// Convert intensity in Candela to Lux.
        /// </summary>
        /// <param name="candela">Intensity in Lux.</param>
        /// <param name="distance">Distance between light and surface.</param>
        /// <returns>Intensity in Lux.</returns>
        public static float CandelaToLux(float candela, float distance)
        {
            return candela * distance * distance;
        }

        /// <summary>
        /// Convert intensity in Ev100 to Nits.
        /// </summary>
        /// <param name="ev100">Intensity in Ev100.</param>
        /// <returns>Intensity in Nits.</returns>
        public static float Ev100ToNits(float ev100)
        {
            return Mathf.Pow(2.0f, ev100 + k_EvToLuminanceFactor);
        }

        /// <summary>
        /// Convert intensity in Nits to Ev100.
        /// </summary>
        /// <param name="nits">Intensity in Nits.</param>
        /// <returns>Intensity in Ev100.</returns>
        public static float NitsToEv100(float nits)
        {
            return Mathf.Log(nits, 2) + k_LuminanceToEvFactor;
        }

        /// <summary>
        /// Convert intensity in Ev100 to Candela.
        /// </summary>
        /// <param name="ev100">Intensity in Ev100.</param>
        /// <returns>Intensity in Candela.</returns>
        public static float Ev100ToCandela(float ev100)
        {
            return Ev100ToNits(ev100);
        }

        /// <summary>
        /// Convert intensity in Candela to Ev100.
        /// </summary>
        /// <param name="candela">Intensity in Candela.</param>
        /// <returns>Intensity in Ev100.</returns>
        public static float CandelaToEv100(float candela)
        {
            return NitsToEv100(candela);
        }

        internal static float ConvertIntensityInternal(float intensity, LightUnit fromUnit, LightUnit toUnit,
            LightType lightType, float area, float luxAtDistance, float solidAngle)
        {
            if (!IsLightUnitSupported(lightType, fromUnit) || !IsLightUnitSupported(lightType, toUnit))
            {
                throw new ArgumentException("Converting " + fromUnit + " to " + toUnit
                                            + " is undefined for lights of type " + lightType);
            }

            if (fromUnit == toUnit)
            {
                return intensity;
            }

            switch (fromUnit)
            {
                case LightUnit.Lumen:
                {
                    switch (toUnit)
                    {
                        case LightUnit.Candela:
                        {
                            // Lumen => Candela:
                            return LumenToCandela(intensity, solidAngle);
                        }

                        case LightUnit.Lux:
                        {
                            // Lumen => Candela => Lux
                            float candela = LumenToCandela(intensity, solidAngle);
                            return CandelaToLux(candela, luxAtDistance);
                        }

                        case LightUnit.Nits:
                        {
                            // Lumen => Nits
                            return LumenToNits(intensity, area);
                        }

                        case LightUnit.Ev100:
                        {
                            // Lumen => Candela/Nits => Ev100
                            float candelaNits = lightType switch
                            {
                                LightType.Point or LightType.Spot or LightType.Pyramid =>
                                    LumenToCandela(intensity, solidAngle),

                                LightType.Rectangle or LightType.Disc or LightType.Tube =>
                                    LumenToNits(intensity, area),

                                _ =>
                                    throw new ArgumentException("Converting from Lumen to Ev100 is undefined for light type "
                                                                 + lightType)
                            };
                            return NitsToEv100(candelaNits);
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(toUnit), toUnit, null);
                    }
                }

                case LightUnit.Candela:
                {
                    switch (toUnit)
                    {
                        case LightUnit.Lumen:
                        {
                            // Candela => Lumen
                            return CandelaToLumen(intensity, solidAngle);
                        }

                        case LightUnit.Lux:
                        {
                            // Candela => Lux
                            return CandelaToLux(intensity, luxAtDistance);
                        }

                        case LightUnit.Ev100:
                        {
                            // Candela => Ev100
                            return NitsToEv100(intensity);
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(toUnit), toUnit, null);
                    }
                }

                case LightUnit.Lux:
                {
                    switch (toUnit)
                    {
                        case LightUnit.Lumen:
                        {
                            // Lux => Candela => Lumen
                            float candela = LuxToCandela(intensity, luxAtDistance);
                            return CandelaToLumen(candela, solidAngle);
                        }

                        case LightUnit.Candela:
                        {
                            // Lux => Candela
                            return LuxToCandela(intensity, luxAtDistance);
                        }

                        case LightUnit.Ev100:
                        {
                            // Lux => Candela => Ev100
                            float candela = LuxToCandela(intensity, luxAtDistance);
                            return NitsToEv100(candela);
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(toUnit), toUnit, null);
                    }
                }

                case LightUnit.Nits:
                {
                    switch (toUnit)
                    {
                        case LightUnit.Lumen:
                        {
                            return NitsToLumen(intensity, area);
                        }

                        case LightUnit.Ev100:
                        {
                            return NitsToEv100(intensity);
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(toUnit), toUnit, null);
                    }
                }

                case LightUnit.Ev100:
                {
                    switch (toUnit)
                    {
                        case LightUnit.Lumen:
                        {
                            // Ev100 => Candela/Nits => Lumen
                            float candelaOrNits = Ev100ToNits(intensity);
                            return lightType switch
                            {
                                LightType.Point or LightType.Spot or LightType.Pyramid =>
                                    CandelaToLumen(candelaOrNits, solidAngle),

                                LightType.Rectangle or LightType.Disc or LightType.Tube =>
                                    NitsToLumen(candelaOrNits, area),

                                _ =>
                                    throw new ArgumentException("Converting from Lumen to Ev100 is undefined for light type "
                                                                + lightType)
                            };
                        }

                        case LightUnit.Nits:
                        case LightUnit.Candela:
                        {
                            // Ev100 => Candela/Nits
                            return Ev100ToNits(intensity);
                        }

                        case LightUnit.Lux:
                        {
                            // Ev100 => Candela => Lux
                            float candela = Ev100ToNits(intensity);
                            return CandelaToLux(candela, luxAtDistance);
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(toUnit), toUnit, null);
                    }
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(fromUnit), fromUnit, null);
            }
        }

        /// <summary>
        /// Convert intensity from one unit to another using the parameters of a given Light.
        /// </summary>
        /// <param name="light">Light to use parameters from.</param>
        /// <param name="intensity">Intensity to be converted.</param>
        /// <param name="fromUnit">Unit to convert from.</param>
        /// <param name="toUnit">Unit to convert to.</param>
        /// <returns>Converted intensity.</returns>
        public static float ConvertIntensity(Light light, float intensity, LightUnit fromUnit, LightUnit toUnit)
        {
            LightType lightType = light.type;
            float area = lightType switch
            {
                LightType.Rectangle => GetAreaFromRectangleLight(light.areaSize),
                LightType.Disc => GetAreaFromDiscLight(light.areaSize.x), // Disc radius is stored in areaSize.x
                LightType.Tube => GetAreaFromTubeLight(light.areaSize.x), // Tube length is stored in areaSize.x
                _ => 0.0f
            };
            float luxAtDistance = light.luxAtDistance;
            float solidAngle = lightType switch
            {
                LightType.Spot or LightType.Pyramid or LightType.Point => GetSolidAngle(lightType, light.enableSpotReflector,
                    light.spotAngle, light.areaSize.x), // Pyramid aspect ratio is store in areaSize.x
                _ => 0.0f
            };

            return ConvertIntensityInternal(intensity, fromUnit, toUnit, lightType, area, luxAtDistance, solidAngle);
        }
    }
}
