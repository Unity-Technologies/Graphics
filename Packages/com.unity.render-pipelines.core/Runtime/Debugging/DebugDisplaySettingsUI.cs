using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The UI implementation for a debug settings panel
    /// </summary>
    public class DebugDisplaySettingsUI : IDebugData
    {
        private IEnumerable<IDebugDisplaySettingsPanelDisposable> m_DisposablePanels;
        private IDebugDisplaySettings m_Settings;

        private void Reset()
        {
            if (m_Settings != null)
            {
                m_Settings.Reset();

                // TODO: Tear the UI down and re-create it for now - this is horrible, so reset it instead.
                UnregisterDebug();
                RegisterDebug(m_Settings);
                DebugManager.instance.RefreshEditor();
            }
        }

        /// <summary>
        /// Register a display for the UI
        /// </summary>
        /// <param name="settings"><see cref="IDebugDisplaySettings"/> to be registered</param>
        public void RegisterDebug(IDebugDisplaySettings settings)
        {
            DebugManager debugManager = DebugManager.instance;
            List<IDebugDisplaySettingsPanelDisposable> panels = new List<IDebugDisplaySettingsPanelDisposable>();

            debugManager.RegisterData(this);

            m_Settings = settings;
            m_DisposablePanels = panels;

            Action<IDebugDisplaySettingsData> onExecute = (data) =>
            {
                IDebugDisplaySettingsPanelDisposable disposableSettingsPanel = data.CreatePanel();

                DebugUI.Widget[] panelWidgets = disposableSettingsPanel.Widgets;

                DebugUI.Panel panel = debugManager.GetPanel(
                    displayName: disposableSettingsPanel.PanelName,
                    createIfNull: true,
                    groupIndex: (disposableSettingsPanel is DebugDisplaySettingsPanel debugDisplaySettingsPanel) ? debugDisplaySettingsPanel.Order : 0);

                ObservableList<DebugUI.Widget> panelChildren = panel.children;

                panel.flags = disposableSettingsPanel.Flags;
                panels.Add(disposableSettingsPanel);
                panelChildren.Add(panelWidgets);
            };

            m_Settings.ForEach(onExecute);
        }

        /// <summary>
        /// Unregister the debug panels
        /// </summary>
        public void UnregisterDebug()
        {
            DebugManager debugManager = DebugManager.instance;

            foreach (IDebugDisplaySettingsPanelDisposable disposableSettingsPanel in m_DisposablePanels)
            {
                DebugUI.Widget[] panelWidgets = disposableSettingsPanel.Widgets;
                string panelId = disposableSettingsPanel.PanelName;
                DebugUI.Panel panel = debugManager.GetPanel(panelId, true);
                ObservableList<DebugUI.Widget> panelChildren = panel.children;

                disposableSettingsPanel.Dispose();
                panelChildren.Remove(panelWidgets);
            }

            m_DisposablePanels = null;

            debugManager.UnregisterData(this);
        }

        #region IDebugData

        /// <summary>
        /// The reset action to be executed when a Reset of the rendering debugger is need
        /// </summary>
        /// <returns>A <see cref="Action"/> with the restet callback</returns>
        public Action GetReset()
        {
            return Reset;
        }

        #endregion
    }
}
