
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettingsUI : IDebugData
    {
        private IEnumerable<IDebugDisplaySettingsPanelDisposable> m_DisposablePanels;
        private DebugDisplaySettings m_Settings;
        
        private DebugDisplaySettingsTest m_DisplaySettingsTest;

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
            foreach(IDebugDisplaySettingsPanelDisposable disposablePanel in m_DisposablePanels)
            {
                disposablePanel.Dispose();
            }

            m_DisposablePanels = null;

            DebugManager.instance.UnregisterData(this);
        }

        #region IDebugData
        public Action GetReset()
        {
            return () => m_Settings.Reset();
        }
        #endregion
    }
}
