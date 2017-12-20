using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenuManager
    {
        private static DebugMenuManager s_Instance = null;

        static public DebugMenuManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new DebugMenuManager();
                }

                return s_Instance;
            }
        }

        List<DebugPanel>    m_DebugPanels = new List<DebugPanel>();
        DebugPanel          m_PersistentDebugPanel = null;
        DebugMenuUI         m_DebugMenuUI = null;

        public int          panelCount          { get { return m_DebugPanels.Count; } }
        public DebugMenuUI  menuUI              { get { return m_DebugMenuUI; } }

        private DebugMenuState m_DebugMenuState = null;
        private bool m_DebugMenuStateDirty = false;

        DebugMenuManager()
        {
            m_PersistentDebugPanel = new DebugPanel<DebugPanelUI>("Persistent");
            m_DebugMenuUI = new DebugMenuUI(this);

            LookUpDebugPanelClasses();

            var updater = GameObject.Find("DebugMenuUpdater");
            if (updater == null)
            {
                GameObject go = new GameObject("DebugMenuUpdater");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<DebugMenuUpdater>();
            }
        }

        public DebugPanel GetDebugPanel(int index)
        {
            if (index < m_DebugPanels.Count)
                return m_DebugPanels[index];
            else
                return null;
        }

        public DebugPanel GetPersistentDebugPanel()
        {
            return m_PersistentDebugPanel;
        }

        void LookUpDebugPanelClasses()
        {
            // Prepare all debug menus
            var types = CoreUtils.GetAllAssemblyTypes()
                .Where(t => t.IsSubclassOf(typeof(DebugPanel)));

            m_DebugPanels.Clear();

            foreach (var type in types)
            {
                if(!type.IsGenericTypeDefinition)
                    AddDebugPanel((DebugPanel)Activator.CreateInstance(type));
            }
        }

        T GetDebugPanel<T>() where T:DebugPanel
        {
            foreach(DebugPanel menu in m_DebugPanels)
            {
                if (menu is T)
                    return menu as T;
            }

            return null;
        }

        public DebugPanel GetDebugPanel(string name)
        {
            foreach(DebugPanel menu in m_DebugPanels)
            {
                if (menu.name == name)
                    return menu;
            }

            return null;
        }

        public void Update()
        {
            if(m_DebugMenuState != null && m_DebugMenuStateDirty)
            {
                m_DebugMenuStateDirty = false;
                m_DebugMenuState.ReInitializeDebugItemStates();
            }

            m_DebugMenuUI.Update();
        }

        private void AddDebugPanel(DebugPanel panel)
        {
            m_DebugPanels.Add(panel);
            m_DebugMenuUI.AddDebugPanel(panel);
        }

        public void AddDebugItem<DebugPanelType, DebugItemType>(string name, Func<object> getter, Action<object> setter = null, DebugItemFlag flags = DebugItemFlag.None, DebugItemHandler handler = null) where DebugPanelType : DebugPanel
        {
            DebugPanelType debugPanel = GetDebugPanel<DebugPanelType>();
            if (debugPanel != null)
            {
                debugPanel.AddDebugItem<DebugItemType>(name, getter, setter, flags, handler);
            }

            m_DebugMenuStateDirty = true;
        }

        public void AddDebugItem<DebugItemType>(string debugPanelName, string name, Func<object> getter, Action<object> setter = null, DebugItemFlag flags = DebugItemFlag.None, DebugItemHandler handler = null)
        {
            DebugPanel debugPanel = GetDebugPanel(debugPanelName);
            // If the menu does not exist, create a generic one. This way, users don't have to explicitely create a new DebugMenu class if they don't need any particular overriding of default behavior.
            if(debugPanel == null)
            {
                debugPanel = new DebugPanel<DebugPanelUI>(debugPanelName);
                AddDebugPanel(debugPanel);
            }

            if (debugPanel != null)
            {
                debugPanel.AddDebugItem<DebugItemType>(name, getter, setter, flags, handler);
            }

            m_DebugMenuStateDirty = true;
        }

        public void RemoveDebugItem(string debugPanelName, string name)
        {
            DebugPanel debugPanel = GetDebugPanel(debugPanelName);

            if (debugPanel != null)
            {
                DebugItem item = debugPanel.GetDebugItem(name);
                if (item != null)
                    debugPanel.RemoveDebugItem(item);
            }

            m_DebugMenuStateDirty = true;

        }

        public void SetDebugMenuState(DebugMenuState state)
        {
            m_DebugMenuStateDirty = true;
            m_DebugMenuState = state;
        }
    }
}
