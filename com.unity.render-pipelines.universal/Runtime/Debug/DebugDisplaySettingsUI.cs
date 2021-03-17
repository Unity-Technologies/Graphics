using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class DebugDisplaySettingsUI : IDebugData
    {
        private IEnumerable<IDebugDisplaySettingsPanelDisposable> m_DisposablePanels;
        private DebugDisplaySettings m_Settings;

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

        public void RegisterDebug(DebugDisplaySettings settings)
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

                panels.Add(disposableSettingsPanel);
                panelChildren.Add(panelWidgets);
            };

            m_Settings.ForEach(onExecute);
        }

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
        public Action GetReset()
        {
            return Reset;
        }

        #endregion
    }
}
