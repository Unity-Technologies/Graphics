using System;
using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

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
        internal int debugFullScreenModeOutputSizeScreenPercent { get; private set; } = 50;
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

        static class Strings
        {
            public const string RangeValidationSettingsContainerName = "Pixel Range Settings";

            public static readonly NameAndTooltip MapOverlays = new() { name = "Map Overlays", tooltip = "Overlays render pipeline textures to validate the scene." };
            public static readonly NameAndTooltip MapSize = new() { name = "Map Size", tooltip = "Set the size of the render pipeline texture in the scene." };
            public static readonly NameAndTooltip AdditionalWireframeModes = new() { name = "Additional Wireframe Modes", tooltip = "Debug the scene with additional wireframe shader views that are different from those in the scene view." };
            public static readonly NameAndTooltip WireframeNotSupportedWarning = new() { name = "Warning: This platform might not support wireframe rendering.", tooltip = "Some platforms, for example, mobile platforms using OpenGL ES and Vulkan, might not support wireframe rendering." };
            public static readonly NameAndTooltip Overdraw = new() { name = "Overdraw", tooltip = "Debug anywhere pixels are overdrawn on top of each other." };
            public static readonly NameAndTooltip PostProcessing = new() { name = "Post-processing", tooltip = "Override the controls for Post Processing in the scene." };
            public static readonly NameAndTooltip MSAA = new() { name = "MSAA", tooltip = "Use the checkbox to disable MSAA in the scene." };
            public static readonly NameAndTooltip HDR = new() { name = "HDR", tooltip = "Use the checkbox to disable High Dynamic Range in the scene." };
            public static readonly NameAndTooltip PixelValidationMode = new() { name = "Pixel Validation Mode", tooltip = "Choose between modes that validate pixel on screen." };
            public static readonly NameAndTooltip Channels = new() { name = "Channels", tooltip = "Choose the texture channel used to validate the scene." };
            public static readonly NameAndTooltip ValueRangeMin = new() { name = "Value Range Min", tooltip = "Any values set below this field will be considered invalid and will appear red on screen." };
            public static readonly NameAndTooltip ValueRangeMax = new() { name = "Value Range Max", tooltip = "Any values set above this field will be considered invalid and will appear blue on screen." };
        }

        #endregion

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMapOverlays(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MapOverlays,
                autoEnum = typeof(DebugFullScreenMode),
                getter = () => (int)data.debugFullScreenMode,
                setter = (value) => { },
                getIndex = () => (int)data.debugFullScreenMode,
                setIndex = (value) => data.debugFullScreenMode = (DebugFullScreenMode)value
            };

            internal static DebugUI.Widget CreateMapOverlaySize(DebugDisplaySettingsRendering data) => new DebugUI.Container()
            {
                children =
                {
                    new DebugUI.IntField
                    {
                        nameAndTooltip = Strings.MapSize,
                        getter = () => data.debugFullScreenModeOutputSizeScreenPercent,
                        setter = value => data.debugFullScreenModeOutputSizeScreenPercent = value,
                        incStep = 10,
                        min = () => 0,
                        max = () => 100
                    }
                }
            };

            internal static DebugUI.Widget CreateAdditionalWireframeShaderViews(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.AdditionalWireframeModes,
                autoEnum = typeof(WireframeMode),
                getter = () => (int)data.wireframeMode,
                setter = (value) => { },
                getIndex = () => (int)data.wireframeMode,
                setIndex = (value) => data.wireframeMode = (WireframeMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateWireframeNotSupportedWarning(DebugDisplaySettingsRendering data) => new DebugUI.MessageBox
            {
                nameAndTooltip = Strings.WireframeNotSupportedWarning,
                style = DebugUI.MessageBox.Style.Warning,
                isHiddenCallback = () =>
                {
#if UNITY_EDITOR
                    return true;
#else
                    switch (SystemInfo.graphicsDeviceType)
                    {
                        case GraphicsDeviceType.OpenGLES2:
                        case GraphicsDeviceType.OpenGLES3:
                        case GraphicsDeviceType.Vulkan:
                            return data.wireframeMode == WireframeMode.None;
                        default:
                            return true;
                    }
#endif
                }
            };

            internal static DebugUI.Widget CreateOverdraw(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.Overdraw,
                getter = () => data.overdraw,
                setter = (value) => data.overdraw = value
            };

            internal static DebugUI.Widget CreatePostProcessing(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.PostProcessing,
                autoEnum = typeof(DebugPostProcessingMode),
                getter = () => (int)data.debugPostProcessingMode,
                setter = (value) => data.debugPostProcessingMode = (DebugPostProcessingMode)value,
                getIndex = () => (int)data.debugPostProcessingMode,
                setIndex = (value) => data.debugPostProcessingMode = (DebugPostProcessingMode)value
            };

            internal static DebugUI.Widget CreateMSAA(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.MSAA,
                getter = () => data.enableMsaa,
                setter = (value) => data.enableMsaa = value
            };

            internal static DebugUI.Widget CreateHDR(DebugDisplaySettingsRendering data) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.HDR,
                getter = () => data.enableHDR,
                setter = (value) => data.enableHDR = value
            };

            internal static DebugUI.Widget CreatePixelValidationMode(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.PixelValidationMode,
                autoEnum = typeof(DebugValidationMode),
                getter = () => (int)data.validationMode,
                setter = (value) => { },
                getIndex = () => (int)data.validationMode,
                setIndex = (value) => data.validationMode = (DebugValidationMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreatePixelValidationChannels(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.Channels,
                autoEnum = typeof(PixelValidationChannels),
                getter = () => (int)data.validationChannels,
                setter = (value) => { },
                getIndex = () => (int)data.validationChannels,
                setIndex = (value) => data.validationChannels = (PixelValidationChannels)value
            };

            internal static DebugUI.Widget CreatePixelValueRangeMin(DebugDisplaySettingsRendering data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.ValueRangeMin,
                getter = () => data.ValidationRangeMin,
                setter = (value) => data.ValidationRangeMin = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreatePixelValueRangeMax(DebugDisplaySettingsRendering data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.ValueRangeMax,
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
                AddWidget(DebugDisplaySettingsCommon.WidgetFactory.CreateMissingDebugShadersWarning());

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
                        WidgetFactory.CreateMSAA(data),
                        WidgetFactory.CreatePostProcessing(data),
                        WidgetFactory.CreateAdditionalWireframeShaderViews(data),
                        WidgetFactory.CreateWireframeNotSupportedWarning(data),
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
                            displayName = Strings.RangeValidationSettingsContainerName,
                            isHiddenCallback = () => data.validationMode != DebugValidationMode.HighlightOutsideOfRange,
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
