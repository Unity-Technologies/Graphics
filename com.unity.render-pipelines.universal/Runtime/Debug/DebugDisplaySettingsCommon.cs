using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsCommon : IDebugDisplaySettingsData
    {
        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateMissingDebugShadersWarning() => new DebugUI.MessageBox
            {
                displayName = "Warning: the debug shader variants are missing. Ensure that the \"Strip Debug Variants\" option is disabled in URP Global Settings.",
                style = DebugUI.MessageBox.Style.Warning,
                isHiddenCallback = () =>
                {
#if UNITY_EDITOR
                    return true;
#else
                    if (UniversalRenderPipelineGlobalSettings.instance != null)
                        return !UniversalRenderPipelineGlobalSettings.instance.stripDebugVariants;
                    return true;
#endif
                }
            };
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Frequently Used";

            const string k_GoToSectionString = "Go to Section...";

            public SettingsPanel()
            {
                AddWidget(WidgetFactory.CreateMissingDebugShadersWarning());

                var debugDisplaySettings = UniversalRenderPipelineDebugDisplaySettings.Instance;
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
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem>()
                    {
                        new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () => { DebugManager.instance.RequestEditorWindowPanelIndex(1); }
                        }
                    }
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
                    contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem>()
                    {
                        new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () => { DebugManager.instance.RequestEditorWindowPanelIndex(2); }
                        }
                    }
                });

                var renderingSettingsData = debugDisplaySettings.renderingSettings;
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
                        DebugDisplaySettingsRendering.WidgetFactory.CreateWireframeNotSupportedWarning(renderingSettingsData),
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
