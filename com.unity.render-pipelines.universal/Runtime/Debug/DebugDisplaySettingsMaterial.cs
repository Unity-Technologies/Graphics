using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsMaterial : IDebugDisplaySettingsData
    {
        #region Material validation
        internal enum AlbedoDebugValidationPreset
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

        struct AlbedoDebugValidationPresetData
        {
            public string name;
            public Color color;
            public float minLuminance;
            public float maxLuminance;
        }

        AlbedoDebugValidationPresetData[] m_AlbedoDebugValidationPresetData =
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

        AlbedoDebugValidationPreset m_AlbedoDebugValidationPreset;
        internal AlbedoDebugValidationPreset albedoDebugValidationPreset
        {
            get => m_AlbedoDebugValidationPreset;
            set
            {
                m_AlbedoDebugValidationPreset = value;
                AlbedoDebugValidationPresetData presetData = m_AlbedoDebugValidationPresetData[(int)value];
                AlbedoMinLuminance = presetData.minLuminance;
                AlbedoMaxLuminance = presetData.maxLuminance;
                AlbedoCompareColor = presetData.color;
            }
        }

        internal float AlbedoMinLuminance = 0.01f;
        internal float AlbedoMaxLuminance = 0.90f;

        float m_AlbedoHueTolerance = 0.104f;
        internal float AlbedoHueTolerance
        {
            get => m_AlbedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : m_AlbedoHueTolerance;
            private set => m_AlbedoHueTolerance = value;
        }

        float m_AlbedoSaturationTolerance = 0.214f;
        internal float AlbedoSaturationTolerance
        {
            get => m_AlbedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : m_AlbedoSaturationTolerance;
            private set => m_AlbedoSaturationTolerance = value;
        }

        internal Color AlbedoCompareColor = new Color(127f / 255f, 127f / 255f, 127f / 255f, 255f / 255f);

        internal float MetallicMinValue = 0.0f;
        internal float MetallicMaxValue = 0.9f;

        internal DebugMaterialValidationMode MaterialValidationMode;

        #endregion

        internal DebugMaterialMode DebugMaterialModeData { get; private set; }
        internal DebugVertexAttributeMode DebugVertexAttributeIndexData { get; private set; }

        static class Strings
        {
            public const string AlbedoSettingsContainerName = "Albedo Settings";
            public const string MetallicSettingsContainerName = "Metallic Settings";

            public static readonly NameAndTooltip MaterialOverride = new() { name = "Material Override", tooltip = "Use the drop-down to select a Material property to visualize on every GameObject on screen." };
            public static readonly NameAndTooltip VertexAttribute = new() { name = "Vertex Attribute", tooltip = "Use the drop-down to select a 3D GameObject attribute, like Texture Coordinates or Vertex Color, to visualize on screen." };
            public static readonly NameAndTooltip MaterialValidationMode = new() { name = "Material Validation Mode", tooltip = "Debug and validate material properties." };
            public static readonly NameAndTooltip ValidationPreset = new() { name = "Validation Preset", tooltip = "Validate using a list of preset surfaces and inputs based on real-world surfaces." };
            public static readonly NameAndTooltip AlbedoMinLuminance = new() { name = "Min Luminance", tooltip = "Any values set below this field are invalid and appear red on screen." };
            public static readonly NameAndTooltip AlbedoMaxLuminance = new() { name = "Max Luminance", tooltip = "Any values set above this field are invalid and appear blue on screen." };
            public static readonly NameAndTooltip AlbedoHueTolerance = new() { name = "Hue Tolerance", tooltip = "Validate a material based on a specific hue." };
            public static readonly NameAndTooltip AlbedoSaturationTolerance = new() { name = "Saturation Tolerance", tooltip = "Validate a material based on a specific Saturation." };
            public static readonly NameAndTooltip MetallicMinValue = new() { name = "Min Value", tooltip = "Any values set below this field are invalid and appear red on screen." };
            public static readonly NameAndTooltip MetallicMaxValue = new() { name = "Max Value", tooltip = "Any values set above this field are invalid and appear blue on screen." };
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMaterialOverride(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MaterialOverride,
                autoEnum = typeof(DebugMaterialMode),
                getter = () => (int)data.DebugMaterialModeData,
                setter = (value) => { },
                getIndex = () => (int)data.DebugMaterialModeData,
                setIndex = (value) => data.DebugMaterialModeData = (DebugMaterialMode)value
            };

            internal static DebugUI.Widget CreateVertexAttribute(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.VertexAttribute,
                autoEnum = typeof(DebugVertexAttributeMode),
                getter = () => (int)data.DebugVertexAttributeIndexData,
                setter = (value) => { },
                getIndex = () => (int)data.DebugVertexAttributeIndexData,
                setIndex = (value) => data.DebugVertexAttributeIndexData = (DebugVertexAttributeMode)value
            };

            internal static DebugUI.Widget CreateMaterialValidationMode(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MaterialValidationMode,
                autoEnum = typeof(DebugMaterialValidationMode),
                getter = () => (int)data.MaterialValidationMode,
                setter = (value) => { },
                getIndex = () => (int)data.MaterialValidationMode,
                setIndex = (value) => data.MaterialValidationMode = (DebugMaterialValidationMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateAlbedoPreset(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.ValidationPreset,
                autoEnum = typeof(AlbedoDebugValidationPreset),
                getter = () => (int)data.albedoDebugValidationPreset,
                setter = (value) => { },
                getIndex = () => (int)data.albedoDebugValidationPreset,
                setIndex = (value) => data.albedoDebugValidationPreset = (AlbedoDebugValidationPreset)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateAlbedoMinLuminance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoMinLuminance,
                getter = () => data.AlbedoMinLuminance,
                setter = (value) => data.AlbedoMinLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoMaxLuminance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoMaxLuminance,
                getter = () => data.AlbedoMaxLuminance,
                setter = (value) => data.AlbedoMaxLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoHueTolerance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoHueTolerance,
                getter = () => data.AlbedoHueTolerance,
                setter = (value) => data.AlbedoHueTolerance = value,
                incStep = 0.01f,
                isHiddenCallback = () => data.albedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance
            };

            internal static DebugUI.Widget CreateAlbedoSaturationTolerance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoSaturationTolerance,
                getter = () => data.AlbedoSaturationTolerance,
                setter = (value) => data.AlbedoSaturationTolerance = value,
                incStep = 0.01f,
                isHiddenCallback = () => data.albedoDebugValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance
            };

            internal static DebugUI.Widget CreateMetallicMinValue(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.MetallicMinValue,
                getter = () => data.MetallicMinValue,
                setter = (value) => data.MetallicMinValue = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateMetallicMaxValue(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.MetallicMaxValue,
                getter = () => data.MetallicMaxValue,
                setter = (value) => data.MetallicMaxValue = value,
                incStep = 0.01f
            };
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Material";
            public SettingsPanel(DebugDisplaySettingsMaterial data)
            {
                AddWidget(DebugDisplaySettingsCommon.WidgetFactory.CreateMissingDebugShadersWarning());

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Filters",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMaterialOverride(data),
                        WidgetFactory.CreateVertexAttribute(data)
                    }
                });
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Validation",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMaterialValidationMode(data),
                        new DebugUI.Container()
                        {
                            displayName = Strings.AlbedoSettingsContainerName,
                            isHiddenCallback = () => data.MaterialValidationMode != DebugMaterialValidationMode.Albedo,
                            children =
                            {
                                WidgetFactory.CreateAlbedoPreset(data),
                                WidgetFactory.CreateAlbedoMinLuminance(data),
                                WidgetFactory.CreateAlbedoMaxLuminance(data),
                                WidgetFactory.CreateAlbedoHueTolerance(data),
                                WidgetFactory.CreateAlbedoSaturationTolerance(data)
                            }
                        },
                        new DebugUI.Container()
                        {
                            displayName = Strings.MetallicSettingsContainerName,
                            isHiddenCallback = () => data.MaterialValidationMode != DebugMaterialValidationMode.Metallic,
                            children =
                            {
                                WidgetFactory.CreateMetallicMinValue(data),
                                WidgetFactory.CreateMetallicMaxValue(data)
                            }
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive =>
            (DebugMaterialModeData != DebugMaterialMode.None) ||
            (DebugVertexAttributeIndexData != DebugVertexAttributeMode.None) ||
            (MaterialValidationMode != DebugMaterialValidationMode.None);
        public bool IsPostProcessingAllowed => !AreAnySettingsActive;
        public bool IsLightingActive => !AreAnySettingsActive;

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
