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

        bool                m_Enabled = false;
        int                 m_ActivePanelIndex = 0;
        List<DebugPanel>    m_DebugPanels = new List<DebugPanel>();
        DebugPanel          m_PersistentDebugPanel = null;
        DebugMenuUI         m_DebugMenuUI = null;
        bool                m_UpdateFromItemStateRequired = false;

        public bool isEnabled           { get { return m_Enabled; } }
        public int  activePanelIndex    { get { return m_ActivePanelIndex; } set { m_ActivePanelIndex = value; } }
        public int  panelCount          { get { return m_DebugPanels.Count; } }

        DebugMenuManager()
        {
            LookUpDebugPanelClasses();
            m_PersistentDebugPanel = new DebugPanel("Persistent");
            m_DebugMenuUI = new DebugMenuUI(this);

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
                m_DebugPanels.Add((DebugPanel)Activator.CreateInstance(type));
            }
        }

        public void PreviousDebugPanel()
        {
            m_DebugPanels[m_ActivePanelIndex].SetSelected(false);
            m_ActivePanelIndex = m_ActivePanelIndex - 1;
            if (m_ActivePanelIndex == -1)
                m_ActivePanelIndex = m_DebugPanels.Count - 1;

            m_DebugPanels[m_ActivePanelIndex].SetSelected(true);

            //m_DebugMenuUI.Toggle();
            //m_DebugMenuUI.Toggle();
        }

        public void NextDebugPanel()
        {
            m_DebugPanels[m_ActivePanelIndex].SetSelected(false);
            m_ActivePanelIndex = (m_ActivePanelIndex + 1) % m_DebugPanels.Count;
            m_DebugPanels[m_ActivePanelIndex].SetSelected(true);


            //m_DebugMenuUI.Toggle();
            //m_DebugMenuUI.Toggle();
        }

        public void ToggleMenu()
        {
            m_Enabled = !m_Enabled;

            m_DebugMenuUI.BuildGUI();
            m_DebugMenuUI.Toggle();
            m_DebugPanels[m_ActivePanelIndex].SetSelected(m_Enabled);
        }

        public void OnValidate()
        {
            m_DebugPanels[m_ActivePanelIndex].OnValidate();
        }

        public void OnMakePersistent()
        {
            DebugItem selectedItem = m_DebugPanels[m_ActivePanelIndex].GetSelectedDebugItem();
            if(selectedItem != null && selectedItem.readOnly)
            {
                if(m_PersistentDebugPanel.HasDebugItem(selectedItem))
                {
                    m_PersistentDebugPanel.RemoveDebugItem(selectedItem);
                }
                else
                {
                    m_PersistentDebugPanel.AddDebugItem(selectedItem);
                }
            }

            if(m_PersistentDebugPanel.itemCount == 0)
            {
                m_PersistentDebugPanel.SetSelected(false);
                m_DebugMenuUI.EnablePersistentView(false); // Temp, should just need the above. Wait for background UI to be moved to menu itself
            }
            else
            {
                m_PersistentDebugPanel.SetSelected(true);
                m_DebugMenuUI.EnablePersistentView(true);
            }
        }

        public void OnMoveHorizontal(float value)
        {
            m_DebugPanels[m_ActivePanelIndex].OnMoveHorizontal(value);
        }

        public void OnMoveVertical(float value)
        {
            m_DebugPanels[m_ActivePanelIndex].OnMoveVertical(value);
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
            if (m_ActivePanelIndex != -1)
                m_DebugPanels[m_ActivePanelIndex].Update();

            m_PersistentDebugPanel.Update();

            if(m_UpdateFromItemStateRequired)
            {
                m_UpdateFromItemStateRequired = false;
                m_DebugMenuState.UpdateAllItems();
            }
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
                debugPanel = new DebugPanel(debugPanelName);
                m_DebugPanels.Add(debugPanel);
            }

            if (debugPanel != null)
            {
                debugPanel.AddDebugItem<DebugItemType>(name, getter, setter, dynamicDisplay, handler);
            }
        }
    }
}
