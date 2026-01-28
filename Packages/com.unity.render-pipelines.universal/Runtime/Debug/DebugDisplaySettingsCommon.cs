using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class DebugDisplaySettingsCommon : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        [DisplayInfo(name = "Frequently Used", order = -1)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {

            DebugUI.Foldout.ContextMenuItem AddGoToSectionContextMenuItem(string panelName)
            {
                return new DebugUI.Foldout.ContextMenuItem
                {
                    displayName = $"Open {panelName} Tab...",
                    action = () =>
                    {
                        DebugManager.instance.RequestEditorWindowPanel(panelName);
                    }
                };
            }

            public SettingsPanel()
            {
                AddWidget(new DebugUI.RuntimeDebugShadersMessageBox());

                var debugDisplaySettings = UniversalRenderPipelineDebugDisplaySettings.Instance;
                var renderingSettingsData = debugDisplaySettings.renderingSettings;
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Rendering Debug",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        DebugDisplaySettingsRendering.WidgetFactory.CreateMapOverlays(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateStpDebugViews(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateMapOverlaySize(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateHDR(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateMSAA(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreatePostProcessing(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateAdditionalWireframeShaderViews(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateWireframeNotSupportedWarning(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateOverdrawMode(renderingSettingsData),
                        DebugDisplaySettingsRendering.WidgetFactory.CreateMaxOverdrawCount(renderingSettingsData),
                    },
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem> { AddGoToSectionContextMenuItem("Rendering") }
                });

                var materialSettingsData = debugDisplaySettings.materialSettings;
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Material Filters",
                    isHeader = true,
                    opened = true,
                    children =
                    {
                        DebugDisplaySettingsMaterial.WidgetFactory.CreateMaterialOverride(materialSettingsData)
                    },
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem> { AddGoToSectionContextMenuItem("Material") }
                });

                var lightingSettingsData = debugDisplaySettings.lightingSettings;
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
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem> { AddGoToSectionContextMenuItem("Lighting") }
                });
            }
        }

        #region IDebugDisplaySettingsData

        // All common settings are owned by another panel, so they are treated as inactive here.

        /// <inheritdoc/>
        public bool AreAnySettingsActive => false;

        /// <inheritdoc/>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel();
        }

        #endregion
    }
}
