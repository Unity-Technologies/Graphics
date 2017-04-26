using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenuItem
    {
        public Type             type            { get { return m_Type; } }
        public string           name            { get { return m_Name; } }
        public DebugItemDrawer  drawer          { get { return m_Drawer; } }
        public bool             dynamicDisplay  { get { return m_DynamicDisplay; } }
        public bool             readOnly        { get { return m_Setter == null; } }

        public DebugMenuItem(string name, Type type, Func<object> getter, Action<object> setter, bool dynamicDisplay = false, DebugItemDrawer drawer = null)
        {
            m_Type = type;
            m_Setter = setter;
            m_Getter = getter;
            m_Name = name;
            m_Drawer = drawer;
            m_DynamicDisplay = dynamicDisplay;
        }

        public Type GetItemType()
        {
            return m_Type;
        }

        public void SetValue(object value)
        {
            // Setter can be null for readonly items
            if(m_Setter != null)
            {
                m_Setter(value);
                m_Drawer.ClampValues(m_Getter, m_Setter);
            }
        }

        public object GetValue()
        {
            return m_Getter();
        }

        Func<object>    m_Getter;
        Action<object>  m_Setter;
        Type            m_Type;
        string          m_Name;
        DebugItemDrawer m_Drawer = null;
        bool            m_DynamicDisplay = false;
    }

    public class DebugMenu
    {
        public string name { get { return m_Name; } }
        public int itemCount { get { return m_Items.Count; } }

        protected string m_Name = "Unknown Debug Menu";

        private GameObject m_Root = null;
        private List<DebugMenuItem> m_Items = new List<DebugMenuItem>();
        private List<DebugMenuItemUI> m_ItemsUI = new List<DebugMenuItemUI>();
        private int m_SelectedItem = -1;

        public DebugMenu(string name)
        {
            m_Name = name;
        }

        public DebugMenuItem GetDebugMenuItem(int index)
        {
            if (index >= m_Items.Count || index < 0)
                return null;
            return m_Items[index];
        }

        public DebugMenuItem GetSelectedDebugMenuItem()
        {
            if(m_SelectedItem != -1)
            {
                return m_Items[m_SelectedItem];
            }

            return null;
        }

        public bool HasItem(DebugMenuItem debugItem)
        {
            foreach(var item in m_Items)
            {
                if (debugItem == item)
                    return true;
            }

            return false;
        }

        public void RemoveDebugItem(DebugMenuItem debugItem)
        {
            m_Items.Remove(debugItem);
            RebuildGUI();
        }

        public void AddDebugItem(DebugMenuItem debugItem)
        {
            m_Items.Add(debugItem);
            RebuildGUI();
        }

        // TODO: Move this to UI classes
        public GameObject BuildGUI(GameObject parent)
        {
            m_Root = new GameObject(string.Format("{0}", m_Name));
            m_Root.transform.SetParent(parent.transform);
            m_Root.transform.localPosition = Vector3.zero;
            m_Root.transform.localScale = Vector3.one;

            UI.VerticalLayoutGroup menuVL = m_Root.AddComponent<UI.VerticalLayoutGroup>();
            menuVL.spacing = 5.0f;
            menuVL.childControlWidth = true;
            menuVL.childControlHeight = true;
            menuVL.childForceExpandWidth = true;
            menuVL.childForceExpandHeight = false;

            RectTransform menuVLRectTransform = m_Root.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = new Vector3(0.0f, 0.0f);
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

            RebuildGUI();

            m_Root.SetActive(false);
            return m_Root;
        }

        private void RebuildGUI()
        {
            m_Root.transform.DetachChildren();

            DebugMenuUI.CreateTextElement(string.Format("{0} Title", m_Name), m_Name, 14, TextAnchor.MiddleLeft, m_Root);

            m_ItemsUI.Clear();
            foreach (DebugMenuItem menuItem in m_Items)
            {
                DebugItemDrawer drawer = menuItem.drawer; // Should never be null, we have at least the default drawer
                m_ItemsUI.Add(drawer.BuildGUI(m_Root, menuItem));
            }
        }


        void SetSelectedItem(int index)
        {
            if(m_SelectedItem != -1)
            {
                m_ItemsUI[m_SelectedItem].SetSelected(false);
            }

            m_SelectedItem = index;
            m_ItemsUI[m_SelectedItem].SetSelected(true);
        }

        public void SetSelected(bool value)
        {
            m_Root.SetActive(value);
            if(value)
            {
                if (m_SelectedItem == -1)
                {
                    NextItem();
                }
                else
                    SetSelectedItem(m_SelectedItem);
            }
        }

        int NextItemIndex(int current)
        {
            return (current + 1) % m_Items.Count;
        }

        void NextItem()
        {
            if(m_Items.Count != 0)
            {
                int newSelected = (m_SelectedItem + 1) % m_Items.Count;
                SetSelectedItem(newSelected);
            }
        }

        int PreviousItemIndex(int current)
        {
            int newSelected = current - 1;
            if (newSelected == -1)
                newSelected = m_Items.Count - 1;
            return newSelected;
        }

        void PreviousItem()
        {
            if(m_Items.Count != 0)
            {
                int newSelected = m_SelectedItem - 1;
                if (newSelected == -1)
                    newSelected = m_Items.Count - 1;
                SetSelectedItem(newSelected);
            }
        }

        public void OnMoveHorizontal(float value)
        {
            if(m_SelectedItem != -1 && !m_Items[m_SelectedItem].readOnly)
            {
                if (value > 0.0f)
                    m_ItemsUI[m_SelectedItem].OnIncrement();
                else
                    m_ItemsUI[m_SelectedItem].OnDecrement();
            }
        }

        public void OnMoveVertical(float value)
        {
            if (value > 0.0f)
                PreviousItem();
            else
                NextItem();
        }

        public void OnValidate()
        {
            if (m_SelectedItem != -1 && !m_Items[m_SelectedItem].readOnly)
                m_ItemsUI[m_SelectedItem].OnValidate();
        }

        public void AddDebugMenuItem<ItemType>(string name, Func<object> getter, Action<object> setter, bool dynamicDisplay = false, DebugItemDrawer drawer = null)
        {
            if (drawer == null)
                drawer = new DebugItemDrawer();
            DebugMenuItem newItem = new DebugMenuItem(name, typeof(ItemType), getter, setter, dynamicDisplay, drawer);
            drawer.SetDebugItem(newItem);
            m_Items.Add(newItem);
        }

        public void Update()
        {
            // Can happen if the debug menu has been disabled (all UI is destroyed). We can't test DebugMenuManager directly though because of the persistant debug menu (which is always displayed no matter what)
            if (m_Root == null)
                return;

            foreach(var itemUI in m_ItemsUI)
            {
                if(itemUI.dynamicDisplay)
                    itemUI.Update();
            }
        }
    }

    public class LightingDebugMenu
        : DebugMenu
    {
        public LightingDebugMenu()
            : base("Lighting")
        {
        }
    }
}