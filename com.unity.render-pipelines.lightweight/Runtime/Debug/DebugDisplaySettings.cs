
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    public class DebugDisplaySettings : IDebugData
    {
        private IEnumerable<IDebugDisplaySettingsData> m_Data;
        private IEnumerable<IDebugDisplaySettingsPanelDisposable> m_DisposablePanels;
        
        private DebugDisplaySettingsTest m_DisplaySettingsTest;

        private IEnumerable<IDebugDisplaySettingsData> InitialiseData()
        {
            HashSet<IDebugDisplaySettingsData> newData = new HashSet<IDebugDisplaySettingsData>();

            newData.Add(new DebugDisplaySettingsTest());

            return newData;
        }

        public void RegisterDebug()
        {
            DebugManager debugManager = DebugManager.instance;
            List<IDebugDisplaySettingsPanelDisposable> panels = new List<IDebugDisplaySettingsPanelDisposable>();
            
            debugManager.RegisterData(this);
            
            m_Data = InitialiseData();
            m_DisposablePanels = panels;

            foreach(IDebugDisplaySettingsData data in m_Data)
            {
                IDebugDisplaySettingsPanelDisposable disposableSettingsPanel = data.CreatePanel();
                DebugUI.Widget[] panelWidgets = disposableSettingsPanel.Widgets;
                string panelId = disposableSettingsPanel.PanelName;
                DebugUI.Panel panel = debugManager.GetPanel(panelId, true);
                ObservableList<DebugUI.Widget> panelChildren = panel.children;

                panels.Add(disposableSettingsPanel);
                panelChildren.Add(panelWidgets);
            }
        }

        public void UnregisterDebug()
        {
            foreach(IDebugDisplaySettingsPanelDisposable disposablePanel in m_DisposablePanels)
            {
                disposablePanel.Dispose();
            }

            m_Data = null;
            m_DisposablePanels = null;

            DebugManager.instance.UnregisterData(this);
        }

        #region IDebugData
        public Action GetReset()
        {
            return () => m_Data = InitialiseData();
        }
        #endregion
    }
}
