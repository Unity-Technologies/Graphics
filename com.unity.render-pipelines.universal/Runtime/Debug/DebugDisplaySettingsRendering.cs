using System;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettingsRendering : IDebugDisplaySettingsData
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
            if (wireframeMode == WireframeMode.Wireframe)
            {
                debugSceneOverrideMode = DebugSceneOverrideMode.Wireframe;
            }
            else if (wireframeMode == WireframeMode.SolidWireframe)
            {
                debugSceneOverrideMode = DebugSceneOverrideMode.SolidWireframe;
            }
            else if (wireframeMode == WireframeMode.ShadedWireframe)
            {
                debugSceneOverrideMode = DebugSceneOverrideMode.ShadedWireframe;
            }
            else if (overdraw)
            {
                debugSceneOverrideMode = DebugSceneOverrideMode.Overdraw;
            }
            else
            {
                debugSceneOverrideMode = DebugSceneOverrideMode.None;
            }
        }

        internal DebugFullScreenMode debugFullScreenMode { get; private set; } = DebugFullScreenMode.None;
        internal DebugSceneOverrideMode debugSceneOverrideMode { get; private set; } = DebugSceneOverrideMode.None;
        internal DebugMipInfoMode debugMipInfoMode { get; private set; } = DebugMipInfoMode.None;

        public DebugPostProcessingMode debugPostProcessingMode { get; private set; } = DebugPostProcessingMode.Auto;
        public bool enableMsaa { get; private set; } = true;
        public bool enableHDR { get; private set; } = true;

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

            internal static DebugUI.Widget CreateAdditionalWireframeShaderViews(DebugDisplaySettingsRendering data) => new DebugUI.EnumField
            {
                displayName = "Additional Wireframe Shader Views",
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
                displayName = "Mip Modes Debug",
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
                        // TODO: Map size widget
                        WidgetFactory.CreateHDR(data),
                        WidgetFactory.CreateMipModesDebug(data),
                        WidgetFactory.CreateMSAA(data),
                        WidgetFactory.CreatePostProcessing(data),
                        WidgetFactory.CreateAdditionalWireframeShaderViews(data),
                        WidgetFactory.CreateOverdraw(data)
                    }
                });

                // AddWidget(new DebugUI.Foldout
                // {
                //     displayName = "Pixel Validation",
                //     isHeader = true,
                //     opened = true,
                //     children =
                //     {
                //         // TODO: Pixel validation
                //     }
                // });
            }
        }

        #region IDebugDisplaySettingsData
        public bool AreAnySettingsActive => (debugPostProcessingMode != DebugPostProcessingMode.Auto) ||
        (debugFullScreenMode != DebugFullScreenMode.None) ||
        (debugSceneOverrideMode != DebugSceneOverrideMode.None) ||
        (debugMipInfoMode != DebugMipInfoMode.None);

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
