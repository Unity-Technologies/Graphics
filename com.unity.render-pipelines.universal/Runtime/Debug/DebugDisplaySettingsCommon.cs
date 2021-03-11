using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering
{
    public class DebugDisplaySettingsCommon : IDebugDisplaySettingsData
    {
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Common";

            public SettingsPanel()
            {
                var materialSettingsData = DebugDisplaySettings.Instance.MaterialSettings;
                AddWidget(DebugMaterialSettings.WidgetFactory.CreateMaterialOverride(materialSettingsData));

                var lightingSettingsData = DebugDisplaySettings.Instance.LightingSettings;
                AddWidget(DebugDisplaySettingsLighting.WidgetFactory.CreateLightingMode(lightingSettingsData));
                AddWidget(DebugDisplaySettingsLighting.WidgetFactory.CreateLightingFeatures(lightingSettingsData));

                var renderingSettingsData = DebugDisplaySettings.Instance.RenderingSettings;
                AddWidget(DebugDisplaySettingsRendering.WidgetFactory.CreateHDR(renderingSettingsData));
                AddWidget(DebugDisplaySettingsRendering.WidgetFactory.CreateMSAA(renderingSettingsData));
                AddWidget(DebugDisplaySettingsRendering.WidgetFactory.CreatePostProcessing(renderingSettingsData));
                AddWidget(DebugDisplaySettingsRendering.WidgetFactory.CreateSceneDebugModes(renderingSettingsData));
                // TODO: Overdraw
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
