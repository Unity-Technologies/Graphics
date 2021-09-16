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
                var materialSettingsData = DebugDisplaySettings.Instance.materialSettings;
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

                var lightingSettingsData = DebugDisplaySettings.Instance.lightingSettings;
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

                var renderingSettingsData = DebugDisplaySettings.Instance.renderingSettings;
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

        // All common settings are owned by another panel, so they are treated as inactive here.

        public bool AreAnySettingsActive => false;
        public bool IsPostProcessingAllowed => true;
        public bool IsLightingActive => true;
        public bool TryGetScreenClearColor(ref Color _) => false;

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel();
        }

        #endregion
    }
}
