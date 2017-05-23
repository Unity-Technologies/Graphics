using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugItem
    {
        public Type             type            { get { return m_Type; } }
        public string           name            { get { return m_Name; } }
        public DebugItemHandler handler          { get { return m_Handler; } }
        public bool             dynamicDisplay  { get { return m_DynamicDisplay; } }
        public bool             readOnly        { get { return m_Setter == null; } }

        public DebugItem(string name, Type type, Func<object> getter, Action<object> setter, bool dynamicDisplay = false, DebugItemHandler handler = null)
        {
            m_Type = type;
            m_Setter = setter;
            m_Getter = getter;
            m_Name = name;
            m_Handler = handler;
            m_DynamicDisplay = dynamicDisplay;
        }

        public Type GetItemType()
        {
            return m_Type;
        }

        public void SetValue(object value, bool record = true)
        {
            // Setter can be null for readonly items
            if(m_Setter != null)
            {
                m_Setter(value);
                m_Handler.ClampValues(m_Getter, m_Setter);

                // Update state for serialization/undo
                if(record)
                    m_State.SetValue(m_Getter());
            }
        }

        public object GetValue()
        {
            return m_Getter();
        }

        public void SetDebugItemState(DebugItemState state)
        {
            m_State = state;
        }

        Func<object>        m_Getter;
        Action<object>      m_Setter;
        Type                m_Type;
        string              m_Name;
        DebugItemHandler    m_Handler = null;
        bool                m_DynamicDisplay = false;
        DebugItemState  m_State = null;
    }

    public class DebugPanel
    {
        public string   name { get { return m_Name; } }
        public int      itemCount { get { return m_Items.Count; } }

        protected string            m_Name = "Unknown Debug Menu";
        private GameObject          m_Root = null;
        private List<DebugItem>     m_Items = new List<DebugItem>();
        private List<DebugItemUI>   m_ItemsUI = new List<DebugItemUI>();
        private int                 m_SelectedItem = -1;

        public DebugPanel(string name)
        {
            m_Name = name;
        }

        public DebugItem GetDebugItem(int index)
        {
            if (index >= m_Items.Count || index < 0)
                return null;
            return m_Items[index];
        }

        public DebugItem GetDebugItem(string name)
        {
            return m_Items.Find(x => x.name == name);
        }

        public DebugItem GetSelectedDebugItem()
        {
            if(m_SelectedItem != -1)
            {
                return m_Items[m_SelectedItem];
            }

            return null;
        }

        public bool HasDebugItem(DebugItem debugItem)
        {
            foreach(var item in m_Items)
            {
                if (debugItem == item)
                    return true;
            }

            return false;
        }

        public void RemoveDebugItem(DebugItem debugItem)
        {
            m_Items.Remove(debugItem);
            RebuildGUI();
        }

        public void AddDebugItem(DebugItem debugItem)
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
            foreach (DebugItem debugItem in m_Items)
            {
                DebugItemHandler handler = debugItem.handler; // Should never be null, we have at least the default handler
                m_ItemsUI.Add(handler.BuildGUI(m_Root));
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

        void NextItem()
        {
            if(m_Items.Count != 0)
            {
                int newSelected = (m_SelectedItem + 1) % m_Items.Count;
                SetSelectedItem(newSelected);
            }
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

        public void AddDebugItem<ItemType>(string name, Func<object> getter, Action<object> setter, bool dynamicDisplay = false, DebugItemHandler handler = null)
        {
            if (handler == null)
                handler = new DefaultDebugItemHandler();
            DebugItem newItem = new DebugItem(name, typeof(ItemType), getter, setter, dynamicDisplay, handler);
            handler.SetDebugItem(newItem);
            m_Items.Add(newItem);

            DebugMenuManager dmm = DebugMenuManager.instance;
            DebugItemState itemState = dmm.FindDebugItemState(name, m_Name);
            if(itemState == null)
            {
                itemState = handler.CreateDebugItemState();
                itemState.Initialize(name, m_Name);
                itemState.SetValue(getter());
                dmm.AddDebugItemState(itemState);
            }

            newItem.SetDebugItemState(itemState);
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

    public class LightingDebugPanel
        : DebugPanel
    {
        public LightingDebugPanel()
            : base("Lighting")
        {
        }
    }
}