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

        [DisplayInfo(name = "Frequently Used", order = -1)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            const string k_GoToSectionString = "Go to Section...";

            public override DebugUI.Flags Flags => DebugUI.Flags.FrequentlyUsed;

            public SettingsPanel()
            {
                AddWidget(WidgetFactory.CreateMissingDebugShadersWarning());

                foreach (var widget in DebugManager.instance.GetItems(DebugUI.Flags.FrequentlyUsed))
                {
                    if (widget is DebugUI.Foldout foldout)
                    {
                        if (foldout.contextMenuItems == null)
                            foldout.contextMenuItems = new();

                        foldout.contextMenuItems.Add(new DebugUI.Foldout.ContextMenuItem
                        {
                            displayName = k_GoToSectionString,
                            action = () =>
                            {
                                var debugManger = DebugManager.instance;
                                var panelIndex = debugManger.PanelIndex(foldout.panel.displayName);
                                if (panelIndex >= 0)
                                    DebugManager.instance.RequestEditorWindowPanelIndex(panelIndex);
                            }
                        });
                    }

                    AddWidget(widget);
                }
                    
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
