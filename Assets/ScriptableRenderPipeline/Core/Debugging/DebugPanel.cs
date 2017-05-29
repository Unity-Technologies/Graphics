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
        DebugItemState      m_State = null;
    }

    public class DebugPanel
    {
        public string       name        { get { return m_Name; } }
        public DebugPanelUI panelUI     { get { return m_DebugPanelUI; } }
        public int          itemCount   { get { return m_Items.Count; } }

        protected string            m_Name = "Unknown Debug Menu";
        protected List<DebugItem>   m_Items = new List<DebugItem>();
        protected DebugPanelUI      m_DebugPanelUI = null;

        protected DebugPanel(string name)
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
            m_DebugPanelUI.RebuildGUI();
        }

        public void AddDebugItem(DebugItem debugItem)
        {
            m_Items.Add(debugItem);
            m_DebugPanelUI.RebuildGUI();
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
    }

    public class DebugPanel<DebugPanelUIClass>
        : DebugPanel where DebugPanelUIClass:DebugPanelUI, new()
    {
        public DebugPanel(string name)
            : base(name)
        {
            m_DebugPanelUI = new DebugPanelUIClass();
            m_DebugPanelUI.SetDebugPanel(this);
        }
    }
}