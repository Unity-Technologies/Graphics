using UnityEngine;

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

        static void DebugMaterialValidationModeChanged(DebugUI.Field<int> field, int value)
        {
            // Hacky way to hide non-relevant UI options based on displayNames.
            var mode = (DebugMaterialValidationMode)value;
            var validationWidgets = field.parent.children;
            foreach (var widget in validationWidgets)
            {
                if (mode == DebugMaterialValidationMode.None && (
                    widget.displayName == k_AlbedoSettingsContainerName ||
                    widget.displayName == k_MetallicSettingsContainerName))
                {
                    widget.isHidden = true;
                }
                else if (mode == DebugMaterialValidationMode.Albedo && widget.displayName == k_MetallicSettingsContainerName)
                {
                    widget.isHidden = true;
                }
                else if (mode == DebugMaterialValidationMode.Metallic && widget.displayName == k_AlbedoSettingsContainerName)
                {
                    widget.isHidden = true;
                }
                else
                {
                    widget.isHidden = false;
                }
            }
            DebugManager.instance.ReDrawOnScreenDebug();
        }

        const string k_AlbedoSettingsContainerName = "Albedo Settings";
        const string k_MetallicSettingsContainerName = "Metallic Settings";

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMaterialOverride(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Material Override",
                autoEnum = typeof(DebugMaterialMode),
                getter = () => (int)data.DebugMaterialModeData,
                setter = (value) => {},
                getIndex = () => (int)data.DebugMaterialModeData,
                setIndex = (value) => data.DebugMaterialModeData = (DebugMaterialMode)value
            };

            internal static DebugUI.Widget CreateVertexAttribute(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Vertex Attribute",
                autoEnum = typeof(DebugVertexAttributeMode),
                getter = () => (int)data.DebugVertexAttributeIndexData,
                setter = (value) => {},
                getIndex = () => (int)data.DebugVertexAttributeIndexData,
                setIndex = (value) => data.DebugVertexAttributeIndexData = (DebugVertexAttributeMode)value
            };

            internal static DebugUI.Widget CreateMaterialValidationMode(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Material Validation Mode",
                autoEnum = typeof(DebugMaterialValidationMode),
                getter = () => (int)data.MaterialValidationMode,
                setter = (value) => {},
                getIndex = () => (int)data.MaterialValidationMode,
                setIndex = (value) => data.MaterialValidationMode = (DebugMaterialValidationMode)value,
                onValueChanged = DebugMaterialValidationModeChanged
            };

            internal static DebugUI.Widget CreateAlbedoPreset(DebugDisplaySettingsMaterial data) => new DebugUI.EnumField
            {
                displayName = "Validation Preset",
                autoEnum = typeof(AlbedoDebugValidationPreset),
                getter = () => (int)data.albedoDebugValidationPreset,
                setter = (value) => {},
                getIndex = () => (int)data.albedoDebugValidationPreset,
                setIndex = (value) => data.albedoDebugValidationPreset = (AlbedoDebugValidationPreset)value,
                onValueChanged = (field, value) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateAlbedoMinLuminance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Min Luminance",
                getter = () => data.AlbedoMinLuminance,
                setter = (value) => data.AlbedoMinLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoMaxLuminance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Max Luminance",
                getter = () => data.AlbedoMaxLuminance,
                setter = (value) => data.AlbedoMaxLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoHueTolerance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Hue Tolerance",
                getter = () => data.AlbedoHueTolerance,
                setter = (value) => data.AlbedoHueTolerance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoSaturationTolerance(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Saturation Tolerance",
                getter = () => data.AlbedoSaturationTolerance,
                setter = (value) => data.AlbedoSaturationTolerance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateMetallicMinValue(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Min Value",
                getter = () => data.MetallicMinValue,
                setter = (value) => data.MetallicMinValue = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateMetallicMaxValue(DebugDisplaySettingsMaterial data) => new DebugUI.FloatField
            {
                displayName = "Max Value",
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
                            displayName = k_AlbedoSettingsContainerName,
                            isHidden = true,
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
                            displayName = k_MetallicSettingsContainerName,
                            isHidden = true,
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
