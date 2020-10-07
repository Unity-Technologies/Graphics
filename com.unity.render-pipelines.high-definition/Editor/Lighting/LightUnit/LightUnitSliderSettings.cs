using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    struct LightUnitSliderUIDescriptor
    {
        public LightUnitSliderUIDescriptor(LightUnitSliderUIRange[] valueRanges, float[] sliderDistribution,
            string cautionTooltip, string unitName, bool hasMarkers = true, bool clampValue = false)
            : this(valueRanges, sliderDistribution, cautionTooltip, cautionTooltip, unitName, hasMarkers, clampValue)
        {}

        public LightUnitSliderUIDescriptor(LightUnitSliderUIRange[] valueRanges, float[] sliderDistribution, string belowRangeTooltip,
            string aboveRangeTooltip, string unitName, bool hasMarkers = true, bool clampValue = false)
        {
            this.valueRanges = valueRanges;
            this.belowRangeTooltip = belowRangeTooltip;
            this.aboveRangeTooltip = aboveRangeTooltip;
            this.sliderDistribution = sliderDistribution;
            this.unitName = unitName;
            this.hasMarkers = hasMarkers;
            this.clampValue = clampValue;

            sliderRange = new Vector2(
                this.valueRanges.Min(x => x.value.x),
                this.valueRanges.Max(x => x.value.y)
            );
        }

        public readonly float[] sliderDistribution;
        public readonly LightUnitSliderUIRange[] valueRanges;
        public readonly Vector2 sliderRange;
        public readonly string belowRangeTooltip;
        public readonly string aboveRangeTooltip;
        public readonly string unitName;
        public readonly bool hasMarkers;
        public readonly bool clampValue;
    }

    struct LightUnitSliderUIRange
    {
        public LightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value)
            // If no preset value provided, then by default it is the average of the value range.
            : this(icon, tooltip, value, 0.5f * (value.x + value.y))
        {}

        public LightUnitSliderUIRange(Texture2D icon, string tooltip, Vector2 value, float presetValue)
        {
            this.content = new GUIContent(icon, tooltip);
            this.value = value;

            Debug.Assert(presetValue > value.x && presetValue < value.y, "Preset value is outside the slider value range.");

            // Preset values are arbitrarily chosen by artist, and we must use it instead of
            // deriving it automatically (ie, the value range average).
            this.presetValue = presetValue;
        }

        public static LightUnitSliderUIRange CautionRange(string tooltip, float value) => new LightUnitSliderUIRange
        {
            // Load the buildin caution icon with provided tooltip.
            content = new GUIContent( EditorGUIUtility.TrIconContent("console.warnicon").image, tooltip),
            value = new Vector2(-1, value),
            presetValue = -1
        };

        public GUIContent content;
        public Vector2    value;
        public float      presetValue;
    }

    static class LightUnitSliderDescriptors
    {
        // Lux
        public static LightUnitSliderUIDescriptor LuxDescriptor = new LightUnitSliderUIDescriptor(
        LightUnitValueRanges.LuxValueTable,
        LightUnitSliderDistributions.LuxDistribution,
        LightUnitTooltips.k_SunCaution,
           "Lux"
        );

        // Lumen
        public static LightUnitSliderUIDescriptor LumenDescriptor = new LightUnitSliderUIDescriptor(
            LightUnitValueRanges.LumenValueTable,
            LightUnitSliderDistributions.LumenDistribution,
            LightUnitTooltips.k_PunctualCaution,
            "Lumen"
        );

        // Exposure
        public static LightUnitSliderUIDescriptor ExposureDescriptor = new LightUnitSliderUIDescriptor(
            LightUnitValueRanges.ExposureValueTable,
            LightUnitSliderDistributions.ExposureDistribution,
            LightUnitTooltips.k_ExposureBelowCaution,
            LightUnitTooltips.k_ExposureAboveCaution,
            "EV"
        );

        // Temperature
        public static LightUnitSliderUIDescriptor TemperatureDescriptor = new LightUnitSliderUIDescriptor(
            LightUnitValueRanges.KelvinValueTableNew,
            LightUnitSliderDistributions.ExposureDistribution,
            LightUnitTooltips.k_TemperatureCaution,
            "Kelvin",
            false,
            true
        );

        private static class LightUnitValueRanges
        {
            public static readonly LightUnitSliderUIRange[] LumenValueTable =
            {
                new LightUnitSliderUIRange(LightUnitIcon.ExteriorLight,  LightUnitTooltips.k_PunctualExterior,   new Vector2(3000, 40000), 10000),
                new LightUnitSliderUIRange(LightUnitIcon.InteriorLight,  LightUnitTooltips.k_PunctualInterior,   new Vector2(300,  3000),  1000),
                new LightUnitSliderUIRange(LightUnitIcon.DecorativeLight,LightUnitTooltips.k_PunctualDecorative, new Vector2(15,   300),   100),
                new LightUnitSliderUIRange(LightUnitIcon.Candlelight,    LightUnitTooltips.k_PunctualCandle,     new Vector2(0,    15),    12.5f),
            };

            public static readonly LightUnitSliderUIRange[] LuxValueTable =
            {
                new LightUnitSliderUIRange(LightUnitIcon.BrightSky,     LightUnitTooltips.k_LuxBrightSky,     new Vector2(80000, 120000), 100000),
                new LightUnitSliderUIRange(LightUnitIcon.Overcast,      LightUnitTooltips.k_LuxOvercastSky,   new Vector2(10000, 80000),  20000),
                new LightUnitSliderUIRange(LightUnitIcon.SunriseSunset, LightUnitTooltips.k_LuxSunriseSunset, new Vector2(1,     10000),  5000),
                new LightUnitSliderUIRange(LightUnitIcon.Moonlight,     LightUnitTooltips.k_LuxMoonlight,     new Vector2(0,     1),      0.5f),
            };

            public static readonly LightUnitSliderUIRange[] ExposureValueTable =
            {
                new LightUnitSliderUIRange(LightUnitIcon.BrightSky,     LightUnitTooltips.k_ExposureBrightSky,     new Vector2(12, 16)),
                new LightUnitSliderUIRange(LightUnitIcon.Overcast,      LightUnitTooltips.k_ExposureOvercastSky,   new Vector2(8,  12)),
                new LightUnitSliderUIRange(LightUnitIcon.SunriseSunset, LightUnitTooltips.k_ExposureSunriseSunset, new Vector2(6,   8)),
                new LightUnitSliderUIRange(LightUnitIcon.InteriorLight, LightUnitTooltips.k_ExposureInterior,      new Vector2(3,   6)),
                new LightUnitSliderUIRange(LightUnitIcon.Moonlight,     LightUnitTooltips.k_ExposureMoonlitSky,    new Vector2(0,   3)),
                new LightUnitSliderUIRange(LightUnitIcon.MoonlessNight, LightUnitTooltips.k_ExposureMoonlessNight, new Vector2(-5,  0)),
            };

            public static readonly LightUnitSliderUIRange[] KelvinValueTableNew =
            {
                new LightUnitSliderUIRange(LightUnitIcon.BlueSky,          LightUnitTooltips.k_TemperatureBlueSky,          new Vector2(10000, 20000), 15000),
                new LightUnitSliderUIRange(LightUnitIcon.Shade,            LightUnitTooltips.k_TemperatureShade,            new Vector2(7000,  10000), 8000),
                new LightUnitSliderUIRange(LightUnitIcon.CloudySky,        LightUnitTooltips.k_TemperatureCloudySky,        new Vector2(6000,   7000), 6500),
                new LightUnitSliderUIRange(LightUnitIcon.DirectSunlight,   LightUnitTooltips.k_TemperatureDirectSunlight,   new Vector2(4500,   6000), 5500),
                new LightUnitSliderUIRange(LightUnitIcon.Fluorescent,      LightUnitTooltips.k_TemperatureFluorescent,      new Vector2(3500,   4500), 4000),
                new LightUnitSliderUIRange(LightUnitIcon.IntenseAreaLight, LightUnitTooltips.k_TemperatureIncandescent,     new Vector2(2500,   3500), 3000),
                new LightUnitSliderUIRange(LightUnitIcon.Candlelight,      LightUnitTooltips.k_TemperatureCandle,           new Vector2(1500,   2500), 1900),
            };
        }

        private static class LightUnitSliderDistributions
        {
            // Warning: All of these values need to be kept in sync with their associated descriptor's set of value ranges.
            public static readonly float[] LuxDistribution = {0.0f, 0.05f, 0.5f, 0.9f, 1.0f};

            private const float LumenStep = 1 / 4f;
            public static readonly float[] LumenDistribution =
            {
                0 * LumenStep,
                1 * LumenStep,
                2 * LumenStep,
                3 * LumenStep,
                4 * LumenStep
            };

            private const float ExposureStep = 1 / 6f;
            public static readonly float[] ExposureDistribution =
            {
                0 * ExposureStep,
                1 * ExposureStep,
                2 * ExposureStep,
                3 * ExposureStep,
                4 * ExposureStep,
                5 * ExposureStep,
                6 * ExposureStep
            };
        }

        private static class LightUnitIcon
        {
            static string GetLightUnitIconPath() => HDUtils.GetHDRenderPipelinePath() +
                                                    "/Editor/RenderPipelineResources/Texture/LightUnitIcons/";

            // Note: We do not use the editor resource loading mechanism for light unit icons because we need to skin the icon correctly for the editor theme.
            // Maybe the resource reloader can be improved to support icon loading (thus supporting skinning)?
            static Texture2D GetLightUnitIcon(string name)
            {
                var path = GetLightUnitIconPath() + name + ".png";
                return EditorGUIUtility.TrIconContent(path).image as Texture2D;
            }

            // TODO: Move all light unit icons from the package into the built-in resources.
            public static Texture2D BlueSky          = GetLightUnitIcon("BlueSky");
            public static Texture2D ClearSky         = GetLightUnitIcon("ClearSky");
            public static Texture2D Candlelight      = GetLightUnitIcon("Candlelight");
            public static Texture2D DecorativeLight  = GetLightUnitIcon("DecorativeLight");
            public static Texture2D DirectSunlight   = GetLightUnitIcon("DirectSunlight");
            public static Texture2D ExteriorLight    = GetLightUnitIcon("ExteriorLight");
            public static Texture2D IntenseAreaLight = GetLightUnitIcon("IntenseAreaLight");
            public static Texture2D InteriorLight    = GetLightUnitIcon("InteriorLight");
            public static Texture2D MediumAreaLight  = GetLightUnitIcon("MediumAreaLight");
            public static Texture2D MoonlessNight    = GetLightUnitIcon("MoonlessNight");
            public static Texture2D Moonlight        = GetLightUnitIcon("Moonlight");
            public static Texture2D Overcast         = GetLightUnitIcon("Overcast");
            public static Texture2D CloudySky        = GetLightUnitIcon("CloudySky");
            public static Texture2D SoftAreaLight    = GetLightUnitIcon("SoftAreaLight");
            public static Texture2D SunriseSunset    = GetLightUnitIcon("SunriseSunset");
            public static Texture2D VeryBrightSun    = GetLightUnitIcon("VeryBrightSun");
            public static Texture2D BrightSky        = GetLightUnitIcon("BrightSky");
            public static Texture2D Shade            = GetLightUnitIcon("Shade");
            public static Texture2D Fluorescent      = GetLightUnitIcon("Fluorescent");
        }

        private static class LightUnitTooltips
        {
            // Caution
            public const string k_SunCaution           = "Higher than Sunlight";
            public const string k_PunctualCaution      = "Very high intensity light";
            public const string k_ExposureBelowCaution = "Lower than a moonless scene";
            public const string k_ExposureAboveCaution = "Higher than a sunlit scene";
            public const string k_TemperatureCaution   = "";

            // Lux / Directional
            public const string k_LuxBrightSky       = "High Sun";
            public const string k_LuxOvercastSky     = "Cloudy";
            public const string k_LuxSunriseSunset   = "Low Sun";
            public const string k_LuxMoonlight       = "Moon";

            // Punctual
            public const string k_PunctualExterior   = "Exterior";
            public const string k_PunctualInterior   = "Interior";
            public const string k_PunctualDecorative = "Decorative";
            public const string k_PunctualCandle     = "Candle";

            // Exposure
            public const string k_ExposureBrightSky     = "Sunlit Scene";
            public const string k_ExposureOvercastSky   = "Cloudy Scene";
            public const string k_ExposureSunriseSunset = "Low Sun Scene";
            public const string k_ExposureInterior      = "Interior Scene";
            public const string k_ExposureMoonlitSky    = "Moonlit Scene";
            public const string k_ExposureMoonlessNight = "Moonless Scene";

            // Temperature
            public const string k_TemperatureBlueSky        = "Blue Sky";
            public const string k_TemperatureShade          = "Shade (Clear Sky)";
            public const string k_TemperatureCloudySky      = "Cloudy Skylight";
            public const string k_TemperatureDirectSunlight = "Direct Sunlight";
            public const string k_TemperatureFluorescent    = "Fluorescent Light";
            public const string k_TemperatureIncandescent   = "Incandescent Light";
            public const string k_TemperatureCandle         = "Candlelight";
        }
    }
}
