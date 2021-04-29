using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettingsCommon : IDebugDisplaySettingsData
    {
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Frequently Used";

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
