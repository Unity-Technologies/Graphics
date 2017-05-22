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

        bool            m_Enabled = false;
        int             m_ActiveMenuIndex = 0;
        List<DebugMenu> m_DebugMenus = new List<DebugMenu>();
        DebugMenu       m_PersistentDebugMenu = null;
        DebugMenuUI     m_DebugMenuUI = null;
        bool            m_UpdateFromItemStateRequired = false;

        public bool isEnabled       { get { return m_Enabled; } }
        public int activeMenuIndex  { get { return m_ActiveMenuIndex; } set { m_ActiveMenuIndex = value; } }
        public int  menuCount       { get { return m_DebugMenus.Count; } }

        DebugMenuManager()
        {
            LookUpDebugMenuClasses();
            m_PersistentDebugMenu = new DebugMenu("Persistent");
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

        public DebugMenuItemState FindDebugItemState(string itemName, string menuName)
        {
            return m_DebugMenuState.FindDebugItemState(itemName, menuName);
        }

        public void AddDebugMenuItemState(DebugMenuItemState state)
        {
            m_DebugMenuState.AddDebugItemState(state);
        }

        public DebugMenu GetDebugMenu(int index)
        {
            if (index < m_DebugMenus.Count)
                return m_DebugMenus[index];
            else
                return null;
        }

        public DebugMenu GetPersistentDebugMenu()
        {
            return m_PersistentDebugMenu;
        }

        void LookUpDebugMenuClasses()
        {
            // Prepare all debug menus
            var types = Assembly.GetAssembly(typeof(DebugMenu)).GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DebugMenu)));

            m_DebugMenus.Clear();

            foreach (var type in types)
            {
                m_DebugMenus.Add((DebugMenu)Activator.CreateInstance(type));
            }
        }

        public void PreviousDebugMenu()
        {
            m_DebugMenus[m_ActiveMenuIndex].SetSelected(false);
            m_ActiveMenuIndex = m_ActiveMenuIndex - 1;
            if (m_ActiveMenuIndex == -1)
                m_ActiveMenuIndex = m_DebugMenus.Count - 1;

            m_DebugMenus[m_ActiveMenuIndex].SetSelected(true);
        }

        public void NextDebugMenu()
        {
            m_DebugMenus[m_ActiveMenuIndex].SetSelected(false);
            m_ActiveMenuIndex = (m_ActiveMenuIndex + 1) % m_DebugMenus.Count;
            m_DebugMenus[m_ActiveMenuIndex].SetSelected(true);
        }

        public void ToggleMenu()
        {
            m_Enabled = !m_Enabled;

            m_DebugMenuUI.BuildGUI();
            m_DebugMenuUI.Toggle();
            m_DebugMenus[m_ActiveMenuIndex].SetSelected(m_Enabled);
        }

        public void OnValidate()
        {
            m_DebugMenus[m_ActiveMenuIndex].OnValidate();
        }

        public void OnMakePersistent()
        {
            DebugMenuItem selectedItem = m_DebugMenus[m_ActiveMenuIndex].GetSelectedDebugMenuItem();
            if(selectedItem != null && selectedItem.readOnly)
            {
                if(m_PersistentDebugMenu.HasItem(selectedItem))
                {
                    m_PersistentDebugMenu.RemoveDebugItem(selectedItem);
                }
                else
                {
                    m_PersistentDebugMenu.AddDebugItem(selectedItem);
                }
            }

            if(m_PersistentDebugMenu.itemCount == 0)
            {
                m_PersistentDebugMenu.SetSelected(false);
                m_DebugMenuUI.EnablePersistentView(false); // Temp, should just need the above. Wait for background UI to be moved to menu itself
            }
            else
            {
                m_PersistentDebugMenu.SetSelected(true);
                m_DebugMenuUI.EnablePersistentView(true);
            }
        }

        public void OnMoveHorizontal(float value)
        {
            m_DebugMenus[m_ActiveMenuIndex].OnMoveHorizontal(value);
        }

        public void OnMoveVertical(float value)
        {
            m_DebugMenus[m_ActiveMenuIndex].OnMoveVertical(value);
        }

        T GetDebugMenu<T>() where T:DebugMenu
        {
            foreach(DebugMenu menu in m_DebugMenus)
            {
                if (menu is T)
                    return menu as T;
            }

            return null;
        }

        public DebugMenu GetDebugMenu(string name)
        {
            foreach(DebugMenu menu in m_DebugMenus)
            {
                if (menu.name == name)
                    return menu;
            }

            return null;
        }

        public void Update()
        {
            if (m_ActiveMenuIndex != -1)
                m_DebugMenus[m_ActiveMenuIndex].Update();

            m_PersistentDebugMenu.Update();

            if(m_UpdateFromItemStateRequired)
            {
                m_UpdateFromItemStateRequired = false;
                m_DebugMenuState.UpdateAllItems();
            }
        }

        public void AddDebugItem<DebugMenuType, ItemType>(string name, Func<object> getter, Action<object> setter = null, bool dynamicDisplay = false, DebugItemHandler handler = null) where DebugMenuType : DebugMenu
        {
            DebugMenuType debugMenu = GetDebugMenu<DebugMenuType>();
            if (debugMenu != null)
            {
                debugMenu.AddDebugMenuItem<ItemType>(name, getter, setter, dynamicDisplay, handler);
            }
        }

        public void AddDebugItem<ItemType>(string debugMenuName, string name, Func<object> getter, Action<object> setter = null, bool dynamicDisplay = false, DebugItemHandler handler = null)
        {
            DebugMenu debugMenu = GetDebugMenu(debugMenuName);
            // If the menu does not exist, create a generic one. This way, users don't have to explicitely create a new DebugMenu class if they don't need any particular overriding of default behavior.
            if(debugMenu == null)
            {
                debugMenu = new DebugMenu(debugMenuName);
                m_DebugMenus.Add(debugMenu);
            }

            if (debugMenu != null)
            {
                debugMenu.AddDebugMenuItem<ItemType>(name, getter, setter, dynamicDisplay, handler);
            }
        }
    }
}
