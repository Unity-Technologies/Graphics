using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug data UI, this is the main holder for debug displays
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
        /// Registers a <see cref="IDebugDisplaySettings"/>
        /// </summary>
        /// <param name="settings">The settings to be registered</param>
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
                string panelId = disposableSettingsPanel.PanelName;
                DebugUI.Panel panel = debugManager.GetPanel(panelId, true);
                ObservableList<DebugUI.Widget> panelChildren = panel.children;

                panel.flags = disposableSettingsPanel.Flags;
                panels.Add(disposableSettingsPanel);
                panelChildren.Add(panelWidgets);
            };

            m_Settings.ForEach(onExecute);
        }

        /// <summary>
        /// Clears all the <see cref="IDebugDisplaySettings"/> that were registered
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
        /// Action when a reset is done, called by the DebugManager
        /// </summary>
        /// <returns>The <see cref="Action"/> that will be executed on reset</returns>
        public Action GetReset()
        {
            return Reset;
        }

        #endregion
    }
}
