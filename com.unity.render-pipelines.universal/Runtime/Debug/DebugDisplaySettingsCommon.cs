using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsCommon : IDebugDisplaySettingsData
    {
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Frequently Used";

            const string k_GoToSectionString = "Go to Section...";

            public SettingsPanel()
            {
                var materialSettingsData = DebugDisplaySettings.Instance.MaterialSettings;
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Filters",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        DebugDisplaySettingsMaterial.WidgetFactory.CreateMaterialOverride(materialSettingsData)
                    },
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem>()
                    {
                        new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () => { DebugManager.instance.RequestEditorWindowPanelIndex(1); }
                        }
                    }
                });

                var lightingSettingsData = DebugDisplaySettings.Instance.LightingSettings;
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Lighting Debug Modes",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        DebugDisplaySettingsLighting.WidgetFactory.CreateLightingDebugMode(lightingSettingsData),
                        DebugDisplaySettingsLighting.WidgetFactory.CreateLightingFeatures(lightingSettingsData)
                    },
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem>()
                    {
                        new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () => { DebugManager.instance.RequestEditorWindowPanelIndex(2); }
                        }
                    }
                });

                var renderingSettingsData = DebugDisplaySettings.Instance.RenderingSettings;
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Rendering Debug",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        DebugDisplaySettingsRendering.WidgetFactory.CreateHDR(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateMSAA(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreatePostProcessing(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateAdditionalWireframeShaderViews(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateOverdraw(renderingSettingsData)
                    },
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem>()
                    {
                        new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () => { DebugManager.instance.RequestEditorWindowPanelIndex(3); }
                        }
                    }
                });
            }
        }

        #region IDebugDisplaySettingsData

        public bool AreAnySettingsActive => DebugDisplaySettings.Instance.AreAnySettingsActive;
        public bool IsPostProcessingAllowed => DebugDisplaySettings.Instance.IsPostProcessingAllowed;
        public bool IsLightingActive => DebugDisplaySettings.Instance.IsLightingActive;
        public bool TryGetScreenClearColor(ref Color color) => DebugDisplaySettings.Instance.TryGetScreenClearColor(ref color);

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel();
        }

        #endregion
    }
}
