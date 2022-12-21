using System;
using System.Collections.Generic;
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
        [Obsolete("overdraw has been deprecated. Use overdrawMode instead.", false)]

        public bool overdraw
        {
            get => m_Overdraw;
            set
            {
                m_Overdraw = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        DebugOverdrawMode m_OverdrawMode = DebugOverdrawMode.None;

        /// <summary>
        /// Which overdraw debug mode is active.
        /// </summary>
        public DebugOverdrawMode overdrawMode
        {
            get => m_OverdrawMode;
            set
            {
                m_OverdrawMode = value;
                UpdateDebugSceneOverrideMode();
            }
        }

        /// <summary>
        /// Maximum overdraw count for a single pixel.
        ///
        /// This is used to setup the feedback range in when <see cref="overdrawMode"/> is active.
        /// </summary>
        public int maxOverdrawCount { get; set; } = 10;

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
                    sceneOverrideMode = overdrawMode != DebugOverdrawMode.None ? DebugSceneOverrideMode.Overdraw : DebugSceneOverrideMode.None;
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

        /// <summary>
        /// Current Temporal Anti-aliasing debug mode.
        /// </summary>
        public enum TaaDebugMode
        {
            /// <summary>The default non-debug TAA rendering.</summary>
            None = 0,
            /// <summary>Output the jittered raw frame render. TAA current frame influence 100%.</summary>
            ShowRawFrame,
            /// <summary>Output the raw frame render, but with jitter disabled. TAA current frame influence 100%.</summary>
            ShowRawFrameNoJitter,
            /// <summary>Output the clamped (rectified), reprojected TAA history. Current frame influence 0%.</summary>
            ShowClampedHistory,
        }
        /// <summary>
        /// Current TAA debug mode.
        /// </summary>
        public TaaDebugMode taaDebugMode { get; set; } = TaaDebugMode.None;

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
            public static readonly NameAndTooltip OverdrawMode = new() { name = "Overdraw Mode", tooltip = "Debug anywhere materials that overdrawn pixels top of each other." };
            public static readonly NameAndTooltip MaxOverdrawCount = new() { name = "Max Overdraw Count", tooltip = "Maximum overdraw count allowed for a single pixel." };
            public static readonly NameAndTooltip PostProcessing = new() { name = "Post-processing", tooltip = "Override the controls for Post Processing in the scene." };
            public static readonly NameAndTooltip MSAA = new() { name = "MSAA", tooltip = "Use the checkbox to disable MSAA in the scene." };
            public static readonly NameAndTooltip HDR = new() { name = "HDR", tooltip = "Use the checkbox to disable High Dynamic Range in the scene." };
            public static readonly NameAndTooltip TaaDebugMode = new() { name = "TAA Debug Mode", tooltip = "Choose whether to force TAA to output the raw jittered frame or clamped reprojected history." };
            public static readonly NameAndTooltip PixelValidationMode = new() { name = "Pixel Validation Mode", tooltip = "Choose between modes that validate pixel on screen." };
            public static readonly NameAndTooltip Channels = new() { name = "Channels", tooltip = "Choose the texture channel used to validate the scene." };
            public static readonly NameAndTooltip ValueRangeMin = new() { name = "Value Range Min", tooltip = "Any values set below this field will be considered invalid and will appear red on screen." };
            public static readonly NameAndTooltip ValueRangeMax = new() { name = "Value Range Max", tooltip = "Any values set above this field will be considered invalid and will appear blue on screen." };
        }

        #endregion

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMapOverlays(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.MapOverlays,
                autoEnum = typeof(DebugFullScreenMode),
                getter = () => (int)panel.data.fullScreenDebugMode,
                setter = (value) => panel.data.fullScreenDebugMode = (DebugFullScreenMode)value,
                getIndex = () => (int)panel.data.fullScreenDebugMode,
                setIndex = (value) => panel.data.fullScreenDebugMode = (DebugFullScreenMode)value
            };

            internal static DebugUI.Widget CreateMapOverlaySize(SettingsPanel panel) => new DebugUI.Container()
            {
                children =
                {
                    new DebugUI.IntField
                    {
                        nameAndTooltip = Strings.MapSize,
                        getter = () => panel.data.fullScreenDebugModeOutputSizeScreenPercent,
                        setter = value => panel.data.fullScreenDebugModeOutputSizeScreenPercent = value,
                        incStep = 10,
                        min = () => 0,
                        max = () => 100
                    }
                }
            };

            internal static DebugUI.Widget CreateAdditionalWireframeShaderViews(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.AdditionalWireframeModes,
                autoEnum = typeof(DebugWireframeMode),
                getter = () => (int)panel.data.wireframeMode,
                setter = (value) => panel.data.wireframeMode = (DebugWireframeMode)value,
                getIndex = () => (int)panel.data.wireframeMode,
                setIndex = (value) => panel.data.wireframeMode = (DebugWireframeMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreateWireframeNotSupportedWarning(SettingsPanel panel) => new DebugUI.MessageBox
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
                            return panel.data.wireframeMode == DebugWireframeMode.None;
                        default:
                            return true;
                    }
#endif
                }
            };

            internal static DebugUI.Widget CreateOverdrawMode(SettingsPanel panel) => new DebugUI.EnumField()
            {
                nameAndTooltip = Strings.OverdrawMode,
                autoEnum = typeof(DebugOverdrawMode),
                getter = () => (int)panel.data.overdrawMode,
                setter = (value) => panel.data.overdrawMode = (DebugOverdrawMode)value,
                getIndex = () => (int)panel.data.overdrawMode,
                setIndex = (value) => panel.data.overdrawMode = (DebugOverdrawMode)value
            };

            internal static DebugUI.Widget CreateMaxOverdrawCount(SettingsPanel panel) => new DebugUI.Container()
            {
                isHiddenCallback = () => panel.data.overdrawMode == DebugOverdrawMode.None,
                children =
                {
                    new DebugUI.IntField
                    {
                        nameAndTooltip = Strings.MaxOverdrawCount,
                        getter = () => panel.data.maxOverdrawCount,
                        setter = value => panel.data.maxOverdrawCount = value,
                        incStep = 10,
                        min = () => 1,
                        max = () => 500
                    }
                }
            };

            internal static DebugUI.Widget CreatePostProcessing(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.PostProcessing,
                autoEnum = typeof(DebugPostProcessingMode),
                getter = () => (int)panel.data.postProcessingDebugMode,
                setter = (value) => panel.data.postProcessingDebugMode = (DebugPostProcessingMode)value,
                getIndex = () => (int)panel.data.postProcessingDebugMode,
                setIndex = (value) => panel.data.postProcessingDebugMode = (DebugPostProcessingMode)value
            };

            internal static DebugUI.Widget CreateMSAA(SettingsPanel panel) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.MSAA,
                getter = () => panel.data.enableMsaa,
                setter = (value) => panel.data.enableMsaa = value
            };

            internal static DebugUI.Widget CreateHDR(SettingsPanel panel) => new DebugUI.BoolField
            {
                nameAndTooltip = Strings.HDR,
                getter = () => panel.data.enableHDR,
                setter = (value) => panel.data.enableHDR = value
            };

            internal static DebugUI.Widget CreateTaaDebugMode(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.TaaDebugMode,
                autoEnum = typeof(TaaDebugMode),
                getter = () => (int)panel.data.taaDebugMode,
                setter = (value) => panel.data.taaDebugMode = (TaaDebugMode)value,
                getIndex = () => (int)panel.data.taaDebugMode,
                setIndex = (value) => panel.data.taaDebugMode = (TaaDebugMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreatePixelValidationMode(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.PixelValidationMode,
                autoEnum = typeof(DebugValidationMode),
                getter = () => (int)panel.data.validationMode,
                setter = (value) => panel.data.validationMode = (DebugValidationMode)value,
                getIndex = () => (int)panel.data.validationMode,
                setIndex = (value) => panel.data.validationMode = (DebugValidationMode)value,
                onValueChanged = (_, _) => DebugManager.instance.ReDrawOnScreenDebug()
            };

            internal static DebugUI.Widget CreatePixelValidationChannels(SettingsPanel panel) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.Channels,
                autoEnum = typeof(PixelValidationChannels),
                getter = () => (int)panel.data.validationChannels,
                setter = (value) => panel.data.validationChannels = (PixelValidationChannels)value,
                getIndex = () => (int)panel.data.validationChannels,
                setIndex = (value) => panel.data.validationChannels = (PixelValidationChannels)value
            };

            internal static DebugUI.Widget CreatePixelValueRangeMin(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.ValueRangeMin,
                getter = () => panel.data.validationRangeMin,
                setter = (value) => panel.data.validationRangeMin = value,
                incStep = 0.01f
            };

            internal static DebugUI.Widget CreatePixelValueRangeMax(SettingsPanel panel) => new DebugUI.FloatField
            {
                nameAndTooltip = Strings.ValueRangeMax,
                getter = () => panel.data.validationRangeMax,
                setter = (value) => panel.data.validationRangeMax = value,
                incStep = 0.01f
            };
        }

        [DisplayInfo(name = "Rendering", order = 1)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsRendering>
        {
            public SettingsPanel(DebugDisplaySettingsRendering data)
                : base(data)
            {
                AddWidget(DebugDisplaySettingsCommon.WidgetFactory.CreateMissingDebugShadersWarning());

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Rendering Debug",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateMapOverlays(this),
                        WidgetFactory.CreateMapOverlaySize(this),
                        WidgetFactory.CreateHDR(this),
                        WidgetFactory.CreateMSAA(this),
                        WidgetFactory.CreateTaaDebugMode(this),
                        WidgetFactory.CreatePostProcessing(this),
                        WidgetFactory.CreateAdditionalWireframeShaderViews(this),
                        WidgetFactory.CreateWireframeNotSupportedWarning(this),
                        WidgetFactory.CreateOverdrawMode(this),
                        WidgetFactory.CreateMaxOverdrawCount(this),
                    }
                });

                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Pixel Validation",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreatePixelValidationMode(this),
                        new DebugUI.Container()
                        {
                            displayName = Strings.RangeValidationSettingsContainerName,
                            isHiddenCallback = () => data.validationMode != DebugValidationMode.HighlightOutsideOfRange,
                            children =
                            {
                                WidgetFactory.CreatePixelValidationChannels(this),
                                WidgetFactory.CreatePixelValueRangeMin(this),
                                WidgetFactory.CreatePixelValueRangeMax(this)
                            }
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData

        /// <inheritdoc/>
        public bool AreAnySettingsActive => (postProcessingDebugMode != DebugPostProcessingMode.Auto) ||
        (fullScreenDebugMode != DebugFullScreenMode.None) ||
        (sceneOverrideMode != DebugSceneOverrideMode.None) ||
        (mipInfoMode != DebugMipInfoMode.None) ||
        (validationMode != DebugValidationMode.None) ||
        !enableMsaa ||
        !enableHDR ||
        (taaDebugMode != TaaDebugMode.None);

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => (postProcessingDebugMode != DebugPostProcessingMode.Disabled) &&
        (sceneOverrideMode == DebugSceneOverrideMode.None) &&
        (mipInfoMode == DebugMipInfoMode.None);

        /// <inheritdoc/>
        public bool IsLightingActive => (sceneOverrideMode == DebugSceneOverrideMode.None) &&
        (mipInfoMode == DebugMipInfoMode.None);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
