using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [Flags]
    public enum DebugItemFlag
    {
        None = 0,
        DynamicDisplay = 1 << 0,
        EditorOnly = 1 << 1,
        RuntimeOnly = 1 << 2
    }

    public class DebugItem
    {
        static public event Action<DebugItem> OnItemDirty;

        public Type             type            { get { return m_Type; } }
        public string           name            { get { return m_Name; } }
        public string           panelName       { get { return m_PanelName; } }
        public DebugItemHandler handler         { get { return m_Handler; } }
        public DebugItemFlag    flags           { get { return m_Flags; } }
        public bool             readOnly        { get { return m_Setter == null; } }
        public bool             editorOnly      { get { return (flags & DebugItemFlag.EditorOnly) != 0; } }
        public bool             runtimeOnly     { get { return (flags & DebugItemFlag.RuntimeOnly) != 0; } }

        public DebugItem(string name, string panelName, Type type, Func<object> getter, Action<object> setter, DebugItemFlag flags = DebugItemFlag.None, DebugItemHandler handler = null)
        {
            m_Type = type;
            m_Setter = setter;
            m_Getter = getter;
            m_Name = name;
            m_PanelName = panelName;
            m_Handler = handler;
            m_Flags = flags;
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
                m_Handler.ValidateValues(m_Getter, m_Setter);
            }

            if (record && OnItemDirty != null)
                OnItemDirty(this);
        }

        public object GetValue()
        {
            return m_Getter();
        }

        Func<object>        m_Getter;
        Action<object>      m_Setter;
        Type                m_Type;
        string              m_Name;
        string              m_PanelName;
        DebugItemHandler    m_Handler = null;
        DebugItemFlag       m_Flags = DebugItemFlag.None;
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

        public void AddDebugItem<ItemType>(string itemName, Func<object> getter, Action<object> setter, DebugItemFlag flags = DebugItemFlag.None, DebugItemHandler handler = null)
        {
            if (handler == null)
                handler = new DefaultDebugItemHandler();

            DebugItem oldItem = GetDebugItem(itemName);
            if (oldItem != null)
                RemoveDebugItem(oldItem);

            DebugItem newItem = new DebugItem(itemName, m_Name, typeof(ItemType), getter, setter, flags, handler);
            handler.SetDebugItem(newItem);
            m_Items.Add(newItem);
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