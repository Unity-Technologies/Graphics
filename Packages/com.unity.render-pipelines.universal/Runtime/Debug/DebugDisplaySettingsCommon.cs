namespace UnityEngine.Rendering.Universal
{
    class DebugDisplaySettingsCommon : IDebugDisplaySettingsData
    {
        [DisplayInfo(name = "Frequently Used", order = -1)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            const string k_GoToSectionString = "Go to Section...";

            public override DebugUI.Flags Flags => DebugUI.Flags.FrequentlyUsed;

            public SettingsPanel()
            {
                AddWidget(new DebugUI.RuntimeDebugShadersMessageBox());

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
