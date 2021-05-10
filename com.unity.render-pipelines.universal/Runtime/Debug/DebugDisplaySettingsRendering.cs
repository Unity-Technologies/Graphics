using System;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        // Under the hood, the implementation uses a single enum (DebugSceneOverrideMode). For UI, we have split
        // this enum into WireframeMode and a separate Overdraw boolean.

        enum WireframeMode
        {
            None,
            Wireframe,
            SolidWireframe,
            ShadedWireframe,
        }

        WireframeMode m_WireframeMode = WireframeMode.None;
        WireframeMode wireframeMode
        {
            get => m_WireframeMode;
            set
            {
                m_WireframeMode = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        bool m_Overdraw = false;

        bool overdraw
        {
            get => m_Overdraw;
            set
            {
                m_Overdraw = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        void UpdateDebugSceneOverrideMode()
        {
            switch (wireframeMode)
            {
                case WireframeMode.Wireframe:
                    debugSceneOverrideMode = DebugSceneOverrideMode.Wireframe;
                    break;
                case WireframeMode.SolidWireframe:
                    debugSceneOverrideMode = DebugSceneOverrideMode.SolidWireframe;
                    break;
                case WireframeMode.ShadedWireframe:
                    debugSceneOverrideMode = DebugSceneOverrideMode.ShadedWireframe;
                    break;
                default:
                    debugSceneOverrideMode = overdraw ? DebugSceneOverrideMode.Overdraw : DebugSceneOverrideMode.None;
                    break;
            }
        }

        internal DebugFullScreenMode debugFullScreenMode { get; private set; } = DebugFullScreenMode.None;
        internal int debugFullScreenModeOutputSize { get; private set; } = 480;
        internal DebugSceneOverrideMode debugSceneOverrideMode { get; private set; } = DebugSceneOverrideMode.None;
        internal DebugMipInfoMode debugMipInfoMode { get; private set; } = DebugMipInfoMode.None;

        internal DebugPostProcessingMode debugPostProcessingMode { get; private set; } = DebugPostProcessingMode.Auto;
        internal bool enableMsaa { get; private set; } = true;
        internal bool enableHDR { get; private set; } = true;

        #region Pixel validation

        internal DebugValidationMode validationMode { get; private set; }
        internal PixelValidationChannels validationChannels { get; private set; } = PixelValidationChannels.RGB;
        internal float ValidationRangeMin { get; private set; } = 0.0f;
        internal float ValidationRangeMax { get; private set; } = 1.0f;

        const string k_RangeValidationSettingsContainerName = "Pixel Range Settings";

        static void DebugPixelValidationModeChanged(DebugUI.Field<int> field, int value)
        {
            // Hacky way to hide non-relevant UI options based on displayNames.
            var mode = (DebugValidationMode)value;
            var validationWidgets = field.parent.children;
            foreach (var widget in validationWidgets)
            {
                if ((mode == DebugValidationMode.None || mode == DebugValidationMode.HighlightNanInfNegative) &&
                    widget.displayName == k_RangeValidationSettingsContainerName)
                {
                    widget.isHidden = true;
                }
                else if (mode == DebugValidationMode.HighlightOutsideOfRange && widget.displayName == k_RangeValidationSettingsContainerName)
                {
                    widget.isHidden = false;
                }
                else
                {
                    widget.isHidden = false;
                }
            }
            DebugManager.instance.ReDrawOnScreenDebug();
        }

        #endregion

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMapOverlays(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Map Overlays",
                autoEnum = typeof(DebugFullScreenMode),
                getter = () => (int)data.debugFullScreenMode,
                setter = (value) => {},
                getIndex = () => (int)data.debugFullScreenMode,
                setIndex = (value) => data.debugFullScreenMode = (DebugFullScreenMode)value
            };

            internal static DebugUI.Widget CreateMapOverlaySize(DebugDisplaySettingsRendering data) => new DebugUI.Container()
            {
                children =
                {
                    new DebugUI.IntField
                    {
                        displayName = "Map Size",
                        getter = () => data.debugFullScreenModeOutputSize,
                        setter = value => data.debugFullScreenModeOutputSize = value,
                        incStep = 10,
                        min = () => 100,
                        max = () => 2160
                    }
                }
            };

            internal static DebugUI.Widget CreateAdditionalWireframeShaderViews(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Additional Wireframe Modes",
                autoEnum = typeof(WireframeMode),
                getter = () => (int)data.wireframeMode,
                setter = (value) => {},
                getIndex = () => (int)data.wireframeMode,
                setIndex = (value) => data.wireframeMode = (WireframeMode)value
            };

            internal static DebugUI.Widget CreateOverdraw(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                displayName = "Overdraw",
                getter = () => data.overdraw,
                setter = (value) => data.overdraw = value
            };

            internal static DebugUI.Widget CreateMipModesDebug(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Mipmap Debug Mode",
                autoEnum = typeof(DebugMipInfoMode),
                getter = () => (int)data.debugMipInfoMode,
                setter = (value) => {},
                getIndex = () => (int)data.debugMipInfoMode,
                setIndex = (value) => data.debugMipInfoMode = (DebugMipInfoMode)value
            };

            internal static DebugUI.Widget CreatePostProcessing(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Post-processing",
                autoEnum = typeof(DebugPostProcessingMode),
                getter = () => (int)data.debugPostProcessingMode,
                setter = (value) => data.debugPostProcessingMode = (DebugPostProcessingMode)value,
                getIndex = () => (int)data.debugPostProcessingMode,
                setIndex = (value) => data.debugPostProcessingMode = (DebugPostProcessingMode)value
            };

            internal static DebugUI.Widget CreateMSAA(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                displayName = "MSAA",
                getter = () => data.enableMsaa,
                setter = (value) => data.enableMsaa = value
            };

            internal static DebugUI.Widget CreateHDR(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                displayName = "HDR",
                getter = () => data.enableHDR,
                setter = (value) => data.enableHDR = value
            };

            internal static DebugUI.Widget CreatePixelValidationMode(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Pixel Validation Mode",
                autoEnum = typeof(DebugValidationMode),
                getter = () => (int)data.validationMode,
                setter = (value) => {},
                getIndex = () => (int)data.validationMode,
                setIndex = (value) => data.validationMode = (DebugValidationMode)value,
                onValueChanged = DebugPixelValidationModeChanged
            };

            internal static DebugUI.Widget CreatePixelValidationChannels(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Channels",
                autoEnum = typeof(PixelValidationChannels),
                getter = () => (int)data.validationChannels,
                setter = (value) => {},
                getIndex = () => (int)data.validationChannels,
                setIndex = (value) => data.validationChannels = (PixelValidationChannels)value
            };

            internal static DebugUI.Widget CreatePixelValueRangeMin(DebugDisplaySettingsRendering data) => new DebugUI.FloatField
            {
                displayName = "Value Range Min",
                getter = () => data.ValidationRangeMin,
                setter = (value) => data.ValidationRangeMin = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreatePixelValueRangeMax(DebugDisplaySettingsRendering data) => new DebugUI.FloatField
            {
                displayName = "Value Range Max",
                getter = () => data.ValidationRangeMax,
                setter = (value) => data.ValidationRangeMax = value,
                incStep = 0.01f
            };
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Rendering";

            public SettingsPanel(DebugDisplaySettingsRendering data)
            {
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Rendering Debug",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMapOverlays(data),
                        WidgetFactory.CreateMapOverlaySize(data),
                        WidgetFactory.CreateHDR(data),
                        // Mipmap debug modes are unsupported by ShaderGraph and cannot be enabled unless that is addressed.
                        //WidgetFactory.CreateMipModesDebug(data),
                        WidgetFactory.CreateMSAA(data),
                        WidgetFactory.CreatePostProcessing(data),
                        WidgetFactory.CreateAdditionalWireframeShaderViews(data),
                        WidgetFactory.CreateOverdraw(data)
                    }
                });

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Pixel Validation",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreatePixelValidationMode(data),
                        new DebugUI.Container()
                        {
                            displayName = k_RangeValidationSettingsContainerName,
                            isHidden = true,
                            children =
                            {
                                WidgetFactory.CreatePixelValidationChannels(data),
                                WidgetFactory.CreatePixelValueRangeMin(data),
                                WidgetFactory.CreatePixelValueRangeMax(data)
                            }
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (debugPostProcessingMode != DebugPostProcessingMode.Auto) ||
        (debugFullScreenMode != DebugFullScreenMode.None) ||
        (debugSceneOverrideMode != DebugSceneOverrideMode.None) ||
        (debugMipInfoMode != DebugMipInfoMode.None) ||
        (validationMode != DebugValidationMode.None) ||
        !enableMsaa ||
        !enableHDR;

        public bool IsPostProcessingAllowed => (debugPostProcessingMode != DebugPostProcessingMode.Disabled) &&
        (debugSceneOverrideMode == DebugSceneOverrideMode.None) &&
        (debugMipInfoMode == DebugMipInfoMode.None);

        public bool IsLightingActive => (debugSceneOverrideMode == DebugSceneOverrideMode.None) &&
        (debugMipInfoMode == DebugMipInfoMode.None);

        public bool TryGetScreenClearColor(ref Color color)
        {
            switch (debugSceneOverrideMode)
            {
                case DebugSceneOverrideMode.None:
                case DebugSceneOverrideMode.ShadedWireframe:
                    return false;

                case DebugSceneOverrideMode.Overdraw:
                    color = Color.black;
                    return true;

                case DebugSceneOverrideMode.Wireframe:
                case DebugSceneOverrideMode.SolidWireframe:
                    color = new Color(0.1f, 0.1f, 0.1f, 1.0f);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(color));
            }       // End of switch.
        }

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
