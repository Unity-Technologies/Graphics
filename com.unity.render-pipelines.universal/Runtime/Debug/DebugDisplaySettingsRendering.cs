using System;
using UnityEngine;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Debug wireframe modes.
    /// </summary>
    public enum DebugWireframeMode
    {
        /// <summary>No wireframe.</summary>
        None,
        /// <summary>Unfilled wireframe.</summary>
        Wireframe,
        /// <summary>Solid, filled wireframe.</summary>
        SolidWireframe,
        /// <summary>Solid, shaded wireframe.</summary>
        ShadedWireframe,
    }

    /// <summary>
    /// Rendering-related Rendering Debugger settings.
    /// </summary>
    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
    {
        // Under the hood, the implementation uses a single enum (DebugSceneOverrideMode). For UI & public API,
        // we have split this enum into WireframeMode and a separate Overdraw boolean.

        DebugWireframeMode m_WireframeMode = DebugWireframeMode.None;

        /// <summary>
        /// Current debug wireframe mode.
        /// </summary>
        public DebugWireframeMode wireframeMode
        {
            get => m_WireframeMode;
            set
            {
                m_WireframeMode = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        bool m_Overdraw = false;

        /// <summary>
        /// Whether debug overdraw mode is active.
        /// </summary>
        public bool overdraw
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
                case DebugWireframeMode.Wireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.Wireframe;
                    break;
                case DebugWireframeMode.SolidWireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.SolidWireframe;
                    break;
                case DebugWireframeMode.ShadedWireframe:
                    sceneOverrideMode = DebugSceneOverrideMode.ShadedWireframe;
                    break;
                default:
                    sceneOverrideMode = overdraw ? DebugSceneOverrideMode.Overdraw : DebugSceneOverrideMode.None;
                    break;
            }
        }

        /// <summary>
        /// Current debug fullscreen overlay mode.
        /// </summary>
        public DebugFullScreenMode fullScreenDebugMode { get; set; } = DebugFullScreenMode.None;

        /// <summary>
        /// Size of the debug fullscreen overlay, as percentage of the screen size.
        /// </summary>
        public int fullScreenDebugModeOutputSizeScreenPercent { get; set; } = 50;

        internal DebugSceneOverrideMode sceneOverrideMode { get; set; } = DebugSceneOverrideMode.None;
        internal DebugMipInfoMode mipInfoMode { get; set; } = DebugMipInfoMode.None;

        /// <summary>
        /// Current debug post processing mode.
        /// </summary>
        public DebugPostProcessingMode postProcessingDebugMode { get; set; } = DebugPostProcessingMode.Auto;

        /// <summary>
        /// Whether MSAA is enabled.
        /// </summary>
        public bool enableMsaa { get; set; } = true;

        /// <summary>
        /// Whether HDR is enabled.
        /// </summary>
        public bool enableHDR { get; set; } = true;

        #region Pixel validation

        /// <summary>
        /// Current debug pixel validation mode.
        /// </summary>
        public DebugValidationMode validationMode { get; set; }

        /// <summary>
        /// Current validation channels for DebugValidationMode.HighlightOutsideOfRange.
        /// </summary>
        public PixelValidationChannels validationChannels { get; set; } = PixelValidationChannels.RGB;

        /// <summary>
        /// Current minimum threshold value for pixel validation.
        /// Any values below this value will be considered invalid and will appear red on screen.
        /// </summary>
        public float validationRangeMin { get; set; } = 0.0f;

        /// <summary>
        /// Current maximum threshold value for pixel validation.
        /// Any values above this value will be considered invalid and will appear blue on screen.
        /// </summary>
        public float validationRangeMax { get; set; } = 1.0f;

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
                getter = () => (int)data.fullScreenDebugMode,
                setter = (value) => { },
                getIndex = () => (int)data.fullScreenDebugMode,
                setIndex = (value) => data.fullScreenDebugMode = (DebugFullScreenMode)value
            };

            internal static DebugUI.Widget CreateMapOverlaySize(DebugDisplaySettingsRendering data) => new DebugUI.Container()
            {
                children =
                {
                    new DebugUI.IntField
                    {
                        nameAndTooltip = Strings.MapSize,
                        getter = () => data.fullScreenDebugModeOutputSizeScreenPercent,
                        setter = value => data.fullScreenDebugModeOutputSizeScreenPercent = value,
                        incStep = 10,
                        min = () => 0,
                        max = () => 100
                    }
                }
            };

            internal static DebugUI.Widget CreateAdditionalWireframeShaderViews(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.AdditionalWireframeModes,
                autoEnum = typeof(DebugWireframeMode),
                getter = () => (int)data.wireframeMode,
                setter = (value) => { },
                getIndex = () => (int)data.wireframeMode,
                setIndex = (value) => data.wireframeMode = (DebugWireframeMode)value,
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
                            return data.wireframeMode == DebugWireframeMode.None;
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
                getter = () => (int)data.postProcessingDebugMode,
                setter = (value) => data.postProcessingDebugMode = (DebugPostProcessingMode)value,
                getIndex = () => (int)data.postProcessingDebugMode,
                setIndex = (value) => data.postProcessingDebugMode = (DebugPostProcessingMode)value
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
                getter = () => data.validationRangeMin,
                setter = (value) => data.validationRangeMin = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreatePixelValueRangeMax(DebugDisplaySettingsRendering data) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.ValueRangeMax,
                getter = () => data.validationRangeMax,
                setter = (value) => data.validationRangeMax = value,
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
        public bool AreAnySettingsActive => (postProcessingDebugMode != DebugPostProcessingMode.Auto) ||
        (fullScreenDebugMode != DebugFullScreenMode.None) ||
        (sceneOverrideMode != DebugSceneOverrideMode.None) ||
        (mipInfoMode != DebugMipInfoMode.None) ||
        (validationMode != DebugValidationMode.None) ||
        !enableMsaa ||
        !enableHDR;

        public bool IsPostProcessingAllowed => (postProcessingDebugMode != DebugPostProcessingMode.Disabled) &&
        (sceneOverrideMode == DebugSceneOverrideMode.None) &&
        (mipInfoMode == DebugMipInfoMode.None);

        public bool IsLightingActive => (sceneOverrideMode == DebugSceneOverrideMode.None) &&
        (mipInfoMode == DebugMipInfoMode.None);

        public bool TryGetScreenClearColor(ref Color color)
        {
            switch (sceneOverrideMode)
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
            }
        }

        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
