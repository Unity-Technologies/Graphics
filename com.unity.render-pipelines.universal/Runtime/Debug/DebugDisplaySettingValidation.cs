using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsValidation : IDebugDisplaySettingsData
    {
        #region Albedo
        public enum AlbedoDebugValidationPreset
        {
            DefaultLuminance,
            BlackAcrylicPaint,
            DarkSoil,
            WornAsphalt,
            DryClaySoil,
            GreenGrass,
            OldConcrete,
            RedClayTile,
            DrySand,
            NewConcrete,
            WhiteAcrylicPaint,
            FreshSnow,
            BlueSky,
            Foliage,
        }

        public struct AlbedoDebugValidationPresetData
        {
            public string name;
            public Color color;
            public float minLuminance;
            public float maxLuminance;
        }

        AlbedoDebugValidationPresetData[] _albedoDebugValidationPresetData = new AlbedoDebugValidationPresetData[]
        {
            new AlbedoDebugValidationPresetData()
            {
                name = "Default Luminance",
                color = new Color(127f / 255f, 127f / 255f, 127f / 255f),
                minLuminance = 0.01f,
                maxLuminance = 0.90f
            },
            // colors taken from http://www.babelcolor.com/index_htm_files/ColorChecker_RGB_and_spectra.xls
            new AlbedoDebugValidationPresetData()
            {
                name = "Black Acrylic Paint",
                color = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                minLuminance = 0.03f,
                maxLuminance = 0.07f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dark Soil",
                color = new Color(85f / 255f, 61f / 255f, 49f / 255f),
                minLuminance = 0.05f,
                maxLuminance = 0.14f
            },

            new AlbedoDebugValidationPresetData()
            {
                name = "Worn Asphalt",
                color = new Color(91f / 255f, 91f / 255f, 91f / 255f),
                minLuminance = 0.10f,
                maxLuminance = 0.15f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dry Clay Soil",
                color = new Color(137f / 255f, 120f / 255f, 102f / 255f),
                minLuminance = 0.15f,
                maxLuminance = 0.35f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Green Grass",
                color = new Color(123f / 255f, 131f / 255f, 74f / 255f),
                minLuminance = 0.16f,
                maxLuminance = 0.26f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Old Concrete",
                color = new Color(135f / 255f, 136f / 255f, 131f / 255f),
                minLuminance = 0.17f,
                maxLuminance = 0.30f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Red Clay Tile",
                color = new Color(197f / 255f, 125f / 255f, 100f / 255f),
                minLuminance = 0.23f,
                maxLuminance = 0.33f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dry Sand",
                color = new Color(177f / 255f, 167f / 255f, 132f / 255f),
                minLuminance = 0.20f,
                maxLuminance = 0.45f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "New Concrete",
                color = new Color(185f / 255f, 182f / 255f, 175f / 255f),
                minLuminance = 0.32f,
                maxLuminance = 0.55f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "White Acrylic Paint",
                color = new Color(227f / 255f, 227f / 255f, 227f / 255f),
                minLuminance = 0.75f,
                maxLuminance = 0.85f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Fresh Snow",
                color = new Color(243f / 255f, 243f / 255f, 243f / 255f),
                minLuminance = 0.85f,
                maxLuminance = 0.95f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Blue Sky",
                color = new Color(93f / 255f, 123f / 255f, 157f / 255f),
                minLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent - 0.05f,
                maxLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent + 0.05f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Foliage",
                color = new Color(91f / 255f, 108f / 255f, 65f / 255f),
                minLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent - 0.05f,
                maxLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent + 0.05f
            },
        };

        public DebugValidationMode validationMode;
        public float RangeMin = 0.0f;
        public float RangeMax = 1.0f;
        public bool AlsoHighlightAlphaOutsideRange = false;

        AlbedoDebugValidationPreset _albedoDebugValidationPreset;
        public AlbedoDebugValidationPreset albedoDebugValidationPreset
        {
            get => _albedoDebugValidationPreset;
            set
            {
                _albedoDebugValidationPreset = value;
                AlbedoDebugValidationPresetData presetData = _albedoDebugValidationPresetData[(int)value];
                AlbedoMinLuminance = presetData.minLuminance;
                AlbedoMaxLuminance = presetData.maxLuminance;
                AlbedoCompareColor = presetData.color;
            }
        }

        public float AlbedoMinLuminance = 0.01f;
        public float AlbedoMaxLuminance = 0.90f;

        float _albedoHueTolerance = 0.104f;
        public float AlbedoHueTolerance
        {
            get => _albedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : _albedoHueTolerance;
            set => _albedoHueTolerance = value;
        }

        float _albedoSaturationTolerance = 0.214f;
        public float AlbedoSaturationTolerance
        {
            get => _albedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : _albedoSaturationTolerance;
            set => _albedoSaturationTolerance = value;
        }

        public Color AlbedoCompareColor = new Color(127f / 255f, 127f / 255f, 127f / 255f, 255f / 255f);
        #endregion

        #region Metallic
        public float MetallicMinValue = 0.0f;
        public float MetallicMaxValue = 0.9f;
        #endregion

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Validation";

            public SettingsPanel(DebugDisplaySettingsValidation data)
            {
                AddWidget(new DebugUI.EnumField { displayName = "Validation Mode", autoEnum = typeof(DebugValidationMode), getter = () => (int)data.validationMode, setter = (value) => {}, getIndex = () => (int)data.validationMode, setIndex = (value) => data.validationMode = (DebugValidationMode)value});
                AddWidget(new DebugUI.FloatField { displayName = "Pixel Value Range Min", getter = () => data.RangeMin, setter = (value) => data.RangeMin = value});
                AddWidget(new DebugUI.FloatField { displayName = "Pixel Value Range Max", getter = () => data.RangeMax, setter = (value) => data.RangeMax = value});
                AddWidget(new DebugUI.BoolField { displayName = "Highlight Out-Of-Range Alpha", getter = () => data.AlsoHighlightAlphaOutsideRange, setter = (value) => data.AlsoHighlightAlphaOutsideRange = value });

                AddWidget(new DebugUI.EnumField { displayName = "Albedo Luminance Validation Preset", autoEnum = typeof(AlbedoDebugValidationPreset), getter = () => (int)data.albedoDebugValidationPreset, setter = (value) => {}, getIndex = () => (int)data.albedoDebugValidationPreset, setIndex = (value) => data.albedoDebugValidationPreset = (AlbedoDebugValidationPreset)value});
                AddWidget(new DebugUI.FloatField { displayName = "Albedo Min Luminance", getter = () => data.AlbedoMinLuminance, setter = (value) => data.AlbedoMinLuminance = value});
                AddWidget(new DebugUI.FloatField { displayName = "Albedo Max Luminance", getter = () => data.AlbedoMaxLuminance, setter = (value) => data.AlbedoMaxLuminance = value});
                AddWidget(new DebugUI.FloatField { displayName = "Albedo Hue Tolerance", getter = () => data.AlbedoHueTolerance, setter = (value) => data.AlbedoHueTolerance = value});
                AddWidget(new DebugUI.FloatField { displayName = "Albedo Saturation Tolerance", getter = () => data.AlbedoSaturationTolerance, setter = (value) => data.AlbedoSaturationTolerance = value});

                AddWidget(new DebugUI.FloatField { displayName = "Metallic Min Value", getter = () => data.MetallicMinValue, setter = (value) => data.MetallicMinValue = value});
                AddWidget(new DebugUI.FloatField { displayName = "Metallic Max Value", getter = () => data.MetallicMaxValue, setter = (value) => data.MetallicMaxValue = value});
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (validationMode != DebugValidationMode.None);

        public bool IsPostProcessingAllowed => (validationMode == DebugValidationMode.None) ||
        (validationMode == DebugValidationMode.HighlightNanInfNegative) ||
        (validationMode == DebugValidationMode.HighlightOutsideOfRange);

        public bool IsLightingActive => (validationMode == DebugValidationMode.None) ||
        (validationMode == DebugValidationMode.HighlightNanInfNegative) ||
        (validationMode == DebugValidationMode.HighlightOutsideOfRange);

        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
