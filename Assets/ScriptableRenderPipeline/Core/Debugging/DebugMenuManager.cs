using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenuManager
    {
        private static DebugMenuManager s_Instance = null;
        private static string s_MenuStateAssetPath = "Assets/DebugMenuState.asset";

        private DebugMenuState m_DebugMenuState = null;

        static public DebugMenuManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new DebugMenuManager();
                    s_Instance.Initialize();
                }

                return s_Instance;
            }
        }

        List<DebugPanel>    m_DebugPanels = new List<DebugPanel>();
        DebugPanel          m_PersistentDebugPanel = null;
        DebugMenuUI         m_DebugMenuUI = null;
        bool                m_UpdateFromItemStateRequired = false;

        public int          panelCount          { get { return m_DebugPanels.Count; } }
        public DebugMenuUI  menuUI              { get { return m_DebugMenuUI; } }

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

        private void Initialize()
        {
#if UNITY_EDITOR
            m_DebugMenuState = UnityEditor.AssetDatabase.LoadAssetAtPath<DebugMenuState>(s_MenuStateAssetPath);

            if (m_DebugMenuState == null)
            {
                m_DebugMenuState = ScriptableObject.CreateInstance<DebugMenuState>();
                UnityEditor.AssetDatabase.CreateAsset(m_DebugMenuState, s_MenuStateAssetPath);
            }
#endif
        }

        public void RequireUpdateFromDebugItemState()
        {
            m_UpdateFromItemStateRequired = true;
        }

        public DebugItemState FindDebugItemState(string itemName, string menuName)
        {
            return m_DebugMenuState.FindDebugItemState(itemName, menuName);
        }

        public void AddDebugItemState(DebugItemState state)
        {
            m_DebugMenuState.AddDebugItemState(state);
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
            var types = Assembly.GetAssembly(typeof(DebugPanel)).GetTypes()
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
            m_DebugMenuUI.Update();

            if(m_UpdateFromItemStateRequired)
            {
                m_UpdateFromItemStateRequired = false;
                m_DebugMenuState.UpdateAllDebugItems();
            }
        }

        private void AddDebugPanel(DebugPanel panel)
        {
            m_DebugPanels.Add(panel);
            m_DebugMenuUI.AddDebugPanel(panel);
        }

        public void AddDebugItem<DebugPanelType, DebugItemType>(string name, Func<object> getter, Action<object> setter = null, bool dynamicDisplay = false, DebugItemHandler handler = null) where DebugPanelType : DebugPanel
        {
            DebugPanelType debugMenu = GetDebugPanel<DebugPanelType>();
            if (debugMenu != null)
            {
                debugMenu.AddDebugItem<DebugItemType>(name, getter, setter, dynamicDisplay, handler);
            }
        }

        public void AddDebugItem<DebugItemType>(string debugPanelName, string name, Func<object> getter, Action<object> setter = null, bool dynamicDisplay = false, DebugItemHandler handler = null)
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
                debugPanel.AddDebugItem<DebugItemType>(name, getter, setter, dynamicDisplay, handler);
            }
        }
    }
}
