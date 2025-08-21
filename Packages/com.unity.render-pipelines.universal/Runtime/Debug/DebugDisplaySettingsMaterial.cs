using System.Collections.Generic;
using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Material-related Rendering Debugger settings.
    /// </summary>
    public class DebugDisplaySettingsMaterial : IDebugDisplaySettingsData
    {
        #region Material validation

        /// <summary>
        /// Builtin presets for debug albedo validation.
        /// </summary>
        public enum AlbedoDebugValidationPreset
        {
            /// <summary> Use this for default luminance. </summary>
            DefaultLuminance,

            /// <summary> Use this for black acrylic paint. </summary>
            BlackAcrylicPaint,

            /// <summary> Use this for dark soil. </summary>
            DarkSoil,

            /// <summary> Use this for worn asphalt. </summary>
            WornAsphalt,

            /// <summary> Use this for dry clay soil. </summary>
            DryClaySoil,

            /// <summary> Use this for green grass. </summary>
            GreenGrass,

            /// <summary> Use this for old concrete. </summary>
            OldConcrete,

            /// <summary> Use this for red clay tile. </summary>
            RedClayTile,

            /// <summary> Use this for dry sand. </summary>
            DrySand,

            /// <summary> Use this for new concrete. </summary>
            NewConcrete,

            /// <summary> Use this for white acrylic paint. </summary>
            WhiteAcrylicPaint,

            /// <summary> Use this for fresh snow. </summary>
            FreshSnow,

            /// <summary> Use this for blue sky. </summary>
            BlueSky,

            /// <summary> Use this for foliage. </summary>
            Foliage,

            /// <summary> Use this for custom. </summary>
            Custom
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
            new AlbedoDebugValidationPresetData()
            {
                name = "Custom",
                color = new Color(127f / 255f, 127f / 255f, 127f / 255f),
                minLuminance = 0.01f,
                maxLuminance = 0.90f
            },
        };

        AlbedoDebugValidationPreset m_AlbedoValidationPreset;

        /// <summary>
        /// Current albedo debug validation preset.
        /// </summary>
        public AlbedoDebugValidationPreset albedoValidationPreset
        {
            get => m_AlbedoValidationPreset;
            set
            {
                m_AlbedoValidationPreset = value;
                AlbedoDebugValidationPresetData presetData = m_AlbedoDebugValidationPresetData[(int)value];
                albedoMinLuminance = presetData.minLuminance;
                albedoMaxLuminance = presetData.maxLuminance;
                albedoCompareColor = presetData.color;
            }
        }

        /// <summary>
        /// Current minimum luminance threshold value for albedo validation.
        /// Any albedo luminance values below this value will be considered invalid and will appear red on screen.
        /// </summary>
        public float albedoMinLuminance { get; set; } = 0.01f;

        /// <summary>
        /// Current maximum luminance threshold value for albedo validation.
        /// Any albedo luminance values above this value will be considered invalid and will appear blue on screen.
        /// </summary>
        public float albedoMaxLuminance { get; set; } = 0.90f;

        float m_AlbedoHueTolerance = 0.104f;

        /// <summary>
        /// Current hue tolerance value for albedo validation.
        /// </summary>
        public float albedoHueTolerance
        {
            get => m_AlbedoValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : m_AlbedoHueTolerance;
            set => m_AlbedoHueTolerance = value;
        }

        float m_AlbedoSaturationTolerance = 0.214f;

        /// <summary>
        /// Current saturation tolerance value for albedo validation.
        /// </summary>
        public float albedoSaturationTolerance
        {
            get => m_AlbedoValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance ? 1.0f : m_AlbedoSaturationTolerance;
            set => m_AlbedoSaturationTolerance = value;
        }

        /// <summary>
        /// Current target color value for albedo validation.
        /// </summary>
        public Color albedoCompareColor { get; set; } = new Color(127f / 255f, 127f / 255f, 127f / 255f, 255f / 255f);

        /// <summary>
        /// Current minimum threshold value for metallic validation.
        /// Any metallic values below this value will be considered invalid and will appear red on screen.
        /// </summary>
        public float metallicMinValue { get; set; } = 0.0f;

        /// <summary>
        /// Current maximum threshold value for metallic validation.
        /// Any metallic values above this value will be considered invalid and will appear blue on screen.
        /// </summary>
        public float metallicMaxValue { get; set; } = 0.9f;

        /// <summary>
        /// Current value for filtering layers based on the selected light's rendering layers.
        /// </summary>
        public bool renderingLayersSelectedLight { get; set; } = false;

        /// <summary>
        /// Current value for filtering layers based on the selected light's shadow layers.
        /// </summary>
        public bool selectedLightShadowLayerMask { get; set; } = false;

        /// <summary>
        /// Current value for filtering layers.
        /// </summary>
        public uint renderingLayerMask { get; set; } = 0;

        /// <summary>Rendering Layers Debug Colors.</summary>
        public Vector4[] debugRenderingLayersColors = new Vector4[]
        {
            new Vector4(230, 159, 0) / 255,
            new Vector4(86, 180, 233) / 255,
            new Vector4(255, 182, 291) / 255,
            new Vector4(0, 158, 115) / 255,
            new Vector4(240, 228, 66) / 255,
            new Vector4(0, 114, 178) / 255,
            new Vector4(213, 94, 0) / 255,
            new Vector4(170, 68, 170) / 255,
            new Vector4(1.0f, 0.5f, 0.5f),
            new Vector4(0.5f, 1.0f, 0.5f),
            new Vector4(0.5f, 0.5f, 1.0f),
            new Vector4(0.5f, 1.0f, 1.0f),
            new Vector4(0.75f, 0.25f, 1.0f),
            new Vector4(0.25f, 1.0f, 0.75f),
            new Vector4(0.25f, 0.25f, 0.75f),
            new Vector4(0.75f, 0.25f, 0.25f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f),
        };

        /// <summary>
        /// Get the RenderingLayerMask used by the selected light
        /// </summary>
        /// <returns>Bitmask representing the RenderingLayerMask for the selected light.</returns>
        public uint GetDebugLightLayersMask()
        {
#if UNITY_EDITOR
            if (renderingLayersSelectedLight)
            {
                if (UnityEditor.Selection.activeGameObject == null)
                    return 0;
                var light = UnityEditor.Selection.activeGameObject.GetComponent<UniversalAdditionalLightData>();
                if (light == null)
                    return 0;

                if (selectedLightShadowLayerMask)
                    return light.shadowRenderingLayers;
                return light.renderingLayers;
            }
#endif
            return 0xFFFF;
        }

        /// <summary>
        /// Current material validation mode.
        /// </summary>
        public DebugMaterialValidationMode materialValidationMode { get; set; }

        #endregion

        /// <summary>
        /// Current debug material mode.
        /// </summary>
        public DebugMaterialMode materialDebugMode { get; set; }

        /// <summary>
        /// Current debug vertex attribute mode.
        /// </summary>
        public DebugVertexAttributeMode vertexAttributeDebugMode { get; set; }

        static class Strings
        {
            public const string AlbedoSettingsContainerName = "Albedo Settings";
            public const string MetallicSettingsContainerName = "Metallic Settings";
            public const string RenderingLayerMasksSettingsContainerName = "Rendering Layer Masks Settings";

            public static readonly NameAndTooltip MaterialOverride = new() { name = "Material Override", tooltip = "Use the drop-down to select a Material property to visualize on every GameObject on screen." };
            public static readonly NameAndTooltip VertexAttribute = new() { name = "Vertex Attribute", tooltip = "Use the drop-down to select a 3D GameObject attribute, like Texture Coordinates or Vertex Color, to visualize on screen." };
            public static readonly NameAndTooltip MaterialValidationMode = new() { name = "Material Validation Mode", tooltip = "Debug and validate material properties." };
            public static readonly NameAndTooltip RenderingLayersSelectedLight = new() { name = "Filter Rendering Layers by Light", tooltip = "Highlight Renderers affected by Selected Light" };
            public static readonly NameAndTooltip SelectedLightShadowLayerMask = new() { name = "Use Light's Shadow Layer Mask", tooltip = "Highlight Renderers that cast shadows for the Selected Light" };
            public static readonly NameAndTooltip FilterRenderingLayerMask = new() { name = "Filter Layers", tooltip = "Use the dropdown to filter Rendering Layers that you want to visualize" };
            public static readonly NameAndTooltip ValidationPreset = new() { name = "Validation Preset", tooltip = "Validate using a list of preset surfaces and inputs based on real-world surfaces." };
            public static readonly NameAndTooltip AlbedoCustomColor = new() { name = "Target Color", tooltip = "Custom target color for albedo validation." };
            public static readonly NameAndTooltip AlbedoMinLuminance = new() { name = "Min Luminance", tooltip = "Any values set below this field are invalid and appear red on screen." };
            public static readonly NameAndTooltip AlbedoMaxLuminance = new() { name = "Max Luminance", tooltip = "Any values set above this field are invalid and appear blue on screen." };
            public static readonly NameAndTooltip AlbedoHueTolerance = new() { name = "Hue Tolerance", tooltip = "Validate a material based on a specific hue." };
            public static readonly NameAndTooltip AlbedoSaturationTolerance = new() { name = "Saturation Tolerance", tooltip = "Validate a material based on a specific Saturation." };
            public static readonly NameAndTooltip MetallicMinValue = new() { name = "Min Value", tooltip = "Any values set below this field are invalid and appear red on screen." };
            public static readonly NameAndTooltip MetallicMaxValue = new() { name = "Max Value", tooltip = "Any values set above this field are invalid and appear blue on screen." };
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMaterialOverride(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MaterialOverride,
                autoEnum = typeof(DebugMaterialMode),
                getter = () => (int)panel.data.materialDebugMode,
                setter = (value) => panel.data.materialDebugMode = (DebugMaterialMode)value,
                getIndex = () => (int)panel.data.materialDebugMode,
                setIndex = (value) => panel.data.materialDebugMode = (DebugMaterialMode)value
            };

            internal static DebugUI.Widget CreateVertexAttribute(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.VertexAttribute,
                autoEnum = typeof(DebugVertexAttributeMode),
                getter = () => (int)panel.data.vertexAttributeDebugMode,
                setter = (value) => panel.data.vertexAttributeDebugMode = (DebugVertexAttributeMode)value,
                getIndex = () => (int)panel.data.vertexAttributeDebugMode,
                setIndex = (value) => panel.data.vertexAttributeDebugMode = (DebugVertexAttributeMode)value
            };

            internal static DebugUI.Widget CreateMaterialValidationMode(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MaterialValidationMode,
                autoEnum = typeof(DebugMaterialValidationMode),
                getter = () => (int)panel.data.materialValidationMode,
                setter = (value) => panel.data.materialValidationMode = (DebugMaterialValidationMode)value,
                getIndex = () => (int)panel.data.materialValidationMode,
                setIndex = (value) => panel.data.materialValidationMode = (DebugMaterialValidationMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateRenderingLayersSelectedLight (SettingsPanel panel) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.RenderingLayersSelectedLight,
                getter = () => (bool)panel.data.renderingLayersSelectedLight,
                setter = (value) => panel.data.renderingLayersSelectedLight = value,
                flags = DebugUI.Flags.EditorOnly,
            };

            internal static DebugUI.Widget CreateSelectedLightShadowLayerMask(SettingsPanel panel) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.SelectedLightShadowLayerMask,
                getter = () => (bool)panel.data.selectedLightShadowLayerMask,
                setter = value => panel.data.selectedLightShadowLayerMask = value,
                flags = DebugUI.Flags.EditorOnly,
                isHiddenCallback = () => !panel.data.renderingLayersSelectedLight
            };

            internal static DebugUI.RenderingLayerField CreateFilterRenderingLayerMasks(SettingsPanel panel)
            {
                var renderingLayersField = new DebugUI.RenderingLayerField()
                {
                    nameAndTooltip = Strings.FilterRenderingLayerMask,
                    getter = () => panel.data.renderingLayerMask,
                    setter = value => panel.data.renderingLayerMask = value,
                    getRenderingLayerColor = index => panel.data.debugRenderingLayersColors[index],
                    setRenderingLayerColor = (value, index) => panel.data.debugRenderingLayersColors[index] = value,
                    isHiddenCallback = () => panel.data.renderingLayersSelectedLight
                };

                return renderingLayersField;
            }

            internal static DebugUI.Widget CreateAlbedoPreset(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.ValidationPreset,
                autoEnum = typeof(AlbedoDebugValidationPreset),
                getter = () => (int)panel.data.albedoValidationPreset,
                setter = (value) => panel.data.albedoValidationPreset = (AlbedoDebugValidationPreset)value,
                getIndex = () => (int)panel.data.albedoValidationPreset,
                setIndex = (value) => panel.data.albedoValidationPreset = (AlbedoDebugValidationPreset)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateAlbedoCustomColor(SettingsPanel panel) => new DebugUI.ColorField()
            {
                nameAndTooltip = Strings.AlbedoCustomColor,
                getter = () => panel.data.albedoCompareColor,
                setter = (value) => panel.data.albedoCompareColor = value,
                isHiddenCallback = () => panel.data.albedoValidationPreset != AlbedoDebugValidationPreset.Custom
            };

            internal static DebugUI.Widget CreateAlbedoMinLuminance(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoMinLuminance,
                getter = () => panel.data.albedoMinLuminance,
                setter = (value) => panel.data.albedoMinLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoMaxLuminance(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoMaxLuminance,
                getter = () => panel.data.albedoMaxLuminance,
                setter = (value) => panel.data.albedoMaxLuminance = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateAlbedoHueTolerance(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoHueTolerance,
                getter = () => panel.data.albedoHueTolerance,
                setter = (value) => panel.data.albedoHueTolerance = value,
                incStep = 0.01f,
                isHiddenCallback = () => panel.data.albedoValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance
            };

            internal static DebugUI.Widget CreateAlbedoSaturationTolerance(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.AlbedoSaturationTolerance,
                getter = () => panel.data.albedoSaturationTolerance,
                setter = (value) => panel.data.albedoSaturationTolerance = value,
                incStep = 0.01f,
                isHiddenCallback = () => panel.data.albedoValidationPreset == AlbedoDebugValidationPreset.DefaultLuminance
            };

            internal static DebugUI.Widget CreateMetallicMinValue(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.MetallicMinValue,
                getter = () => panel.data.metallicMinValue,
                setter = (value) => panel.data.metallicMinValue = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreateMetallicMaxValue(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.MetallicMaxValue,
                getter = () => panel.data.metallicMaxValue,
                setter = (value) => panel.data.metallicMaxValue = value,
                incStep = 0.01f
            };
        }

        [DisplayInfo(name = "Material", order = 2)]
        [URPHelpURL("features/rendering-debugger-reference", "material")]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsMaterial>
        {
            public SettingsPanel(DebugDisplaySettingsMaterial data)
                : base(data)
            {
                AddWidget(new DebugUI.RuntimeDebugShadersMessageBox());
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Filters",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMaterialOverride(this),
                        new DebugUI.Container()
                        {
                            displayName = Strings.RenderingLayerMasksSettingsContainerName,
                            isHiddenCallback = () => data.materialDebugMode != DebugMaterialMode.RenderingLayerMasks,
                            children =
                            {
                                WidgetFactory.CreateRenderingLayersSelectedLight(this),
                                WidgetFactory.CreateSelectedLightShadowLayerMask(this),
                                WidgetFactory.CreateFilterRenderingLayerMasks(this),
                            }
                        },
                        WidgetFactory.CreateVertexAttribute(this)
                    }
                });
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Validation",
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMaterialValidationMode(this),
                        new DebugUI.Container()
                        {
                            displayName = Strings.AlbedoSettingsContainerName,
                            isHiddenCallback = () => data.materialValidationMode != DebugMaterialValidationMode.Albedo,
                            children =
                            {
                                WidgetFactory.CreateAlbedoPreset(this),
                                WidgetFactory.CreateAlbedoCustomColor(this),
                                WidgetFactory.CreateAlbedoMinLuminance(this),
                                WidgetFactory.CreateAlbedoMaxLuminance(this),
                                WidgetFactory.CreateAlbedoHueTolerance(this),
                                WidgetFactory.CreateAlbedoSaturationTolerance(this)
                            }
                        },
                        new DebugUI.Container()
                        {
                            displayName = Strings.MetallicSettingsContainerName,
                            isHiddenCallback = () => data.materialValidationMode != DebugMaterialValidationMode.Metallic,
                            children =
                            {
                                WidgetFactory.CreateMetallicMinValue(this),
                                WidgetFactory.CreateMetallicMaxValue(this)
                            }
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        public bool AreAnySettingsActive =>
            (materialDebugMode != DebugMaterialMode.None) ||
            (vertexAttributeDebugMode != DebugVertexAttributeMode.None) ||
            (materialValidationMode != DebugMaterialValidationMode.None);

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => !AreAnySettingsActive;

        /// <inheritdoc/>
        public bool IsLightingActive => !AreAnySettingsActive;

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
