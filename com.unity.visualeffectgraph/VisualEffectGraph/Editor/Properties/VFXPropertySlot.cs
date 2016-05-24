using UnityEngine;
using UnityEditor; // Shouldnt be included!
using UnityEditor.Experimental;
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.VFX
{
    public interface VFXPropertySlotObserver
    {
        void OnSlotEvent(VFXPropertySlot.Event type,VFXPropertySlot slot);
    }

    public struct VFXNamedValue
    {
        public VFXNamedValue(string name, VFXExpression value)
        {
            m_Name = name;
            m_Value = value;
        }

        public string m_Name;
        public VFXExpression m_Value;
    }

    public abstract class VFXPropertySlot : VFXUIDataHolder
    {
        public enum Event
        {
            kLinkUpdated,
            kValueUpdated,
        }

        public VFXPropertySlot() {}

        protected void Init<T>(VFXPropertySlot parent, VFXProperty desc) where T : VFXPropertySlot, new()
        {
            m_Desc = desc;
 
            CreateChildren<T>();
            Semantics.CreateValue(this);
            SetDefault();

            // Set the parent at the end
            m_Parent = parent;
        }

        protected void CreateChildren<T>() where T : VFXPropertySlot, new()
        {
            VFXProperty[] children = Semantics.GetChildren();
            if (children != null)
            {
                int nbChildren = children.Length;
                m_Children = new VFXPropertySlot[nbChildren];
                for (int i = 0; i < nbChildren; ++i)
                {
                    VFXPropertySlot child = new T();     
                    child.Init<T>(this, children[i]);
                    m_Children[i] = child;
                }
            }
        }

        public void SetDefault()
        {
            if (!Semantics.Default(this))
                foreach (var child in m_Children)
                    child.SetDefault();
        }

        public int GetNbChildren()
        {
            return m_Children.Length;
        }

        public VFXPropertySlot GetChild(int index)
        {
            return m_Children[index];
        }

        public VFXPropertySlot Parent
        {
            get { return m_Parent; }
        }

        public T Get<T>(bool linked = false)        { return Semantics.Get<T>(this, linked); }
        public void Set<T>(T t,bool linked = false) { Semantics.Set(this,t,linked); }
   
        // Direct access to owned value
        // Prefer using get<T> and Set<T> instead to correctly set value depending on the semantics
        public void SetInnerValue<T>(T t)
        {
            if (m_OwnedValue.Set(t))
                NotifyChange(Event.kValueUpdated);
        }
        public T GetInnerValue<T>() { return m_OwnedValue.Get<T>(); }

        public VFXExpression Value
        {
            set
            {
                if (value != m_OwnedValue)
                {
                    m_OwnedValue = value;
                    NotifyChange(Event.kLinkUpdated); // Link updated as expressions needs to be recomputed 
                }
            }
            get
            {
                return m_OwnedValue;
            }
        }

        public VFXExpression ValueRef
        {
            set { CurrentValueRef.Value = value; }
            get { return CurrentValueRef.Value; }
        }

        public void AddObserver(VFXPropertySlotObserver observer, bool addRecursively = false)
        {
            if (observer != null && !m_Observers.Contains(observer))
                m_Observers.Add(observer);

            if (addRecursively)
                foreach (var child in m_Children)
                    child.AddObserver(observer, true);
        }

        public void RemoveObserver(VFXPropertySlotObserver observer, bool removeRecursively = false)
        {
            m_Observers.Remove(observer);

            if (removeRecursively)
                foreach (var child in m_Children)
                    child.RemoveObserver(observer, true);
        }

        public void NotifyChange(Event type)
        {
            // Invalidate expression cache
            if (m_OwnedValue != null)
            {
                m_OwnedValue.Invalidate();
                m_OwnedValue.Reduce(); // Trigger a reduce but this is TMP as it should be reduced lazily (the model compiler should do it on demand)
            }

            foreach (var observer in m_Observers)
                observer.OnSlotEvent(type, this);

            PropagateChange(type);

            // Invalidate parent's cache and Update parent proxy if any in case of link update
            if (m_Parent != null)
                if (type == Event.kValueUpdated || m_Parent.Semantics.UpdateProxy(m_Parent))
                    m_Parent.NotifyChange(type);
        }

        public virtual void PropagateChange(Event type) {}

        public abstract VFXPropertySlot CurrentValueRef { get; }

        public bool IsValueUsed()
        {
            return CurrentValueRef == this;
        }

        public abstract bool IsLinked();
        public abstract void UnlinkAll();
        public void UnlinkRecursively()
        {
            UnlinkAll();
            foreach (var child in m_Children)
                child.UnlinkRecursively();
        }

        public VFXProperty Property                 { get { return m_Desc; }}
        public string Name                          { get { return m_Desc.m_Name; }}
        public VFXPropertyTypeSemantics Semantics   { get { return m_Desc.m_Type; }}
        public VFXValueType ValueType               { get { return Semantics.ValueType; }}

        public void FlattenValues<T>(List<T> values)
        {
            if (GetNbChildren() == 0)
            {
                if (ValueType == VFXValue.ToValueType<T>())
                    values.Add(Get<T>());
            }
            else foreach (var child in m_Children)
                    child.FlattenValues(values);
        }

        public int ApplyValues<T>(List<T> values,int index = 0)
        {
            if (GetNbChildren() == 0)
            {
                if (ValueType == VFXValue.ToValueType<T>())
                    Set(values[index++]);
            }
            else foreach (var child in m_Children)
                index = child.ApplyValues(values, index);

            return index;
        }

        // Collect all values in the slot hierarchy with its name used in the shader
        // Called from the model compiler
        public void CollectNamedValues(List<VFXNamedValue> values)
        {
            CollectNamedValues(values, "");
        }

        private void CollectNamedValues(List<VFXNamedValue> values,string fullName)
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            VFXExpression refValue = refSlot.Value;
            
            if (refValue != null) // if not null it means value has a concrete type (not kNone)
                values.Add(new VFXNamedValue(AggregateName(fullName,Name), refValue.Reduce())); // TODO Reduce must not be performed here
            else foreach (var child in refSlot.m_Children) // Continue only until we found a value
                    child.CollectNamedValues(values, AggregateName(fullName, Name));
        }

        private string AggregateName(string parent,string child)
        {
            return parent.Length == 0 ? child : parent + "_" + child;
        }

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            m_UIChildrenCollapsed = collapsed;
        }

        public bool UICollapsed { get { return m_UIChildrenCollapsed; } }

        public abstract List<VFXPropertySlot> GetConnectedSlots();

        private VFXExpression m_OwnedValue;

        protected List<VFXPropertySlotObserver> m_Observers = new List<VFXPropertySlotObserver>();

        private VFXProperty m_Desc; // Contains semantic type and name for this value

        protected VFXPropertySlot m_Parent;
        protected VFXPropertySlot[] m_Children = new VFXPropertySlot[0];

        private bool m_UIChildrenCollapsed = true;
    }

    // Concrete implementation for input slot (can be linked to only one output slot)
    public class VFXInputSlot : VFXPropertySlot
    {
        public VFXInputSlot() {}
        public VFXInputSlot(VFXProperty desc)
        {
            Init<VFXInputSlot>(null, desc);  
        }

        public bool Link(VFXOutputSlot slot)
        {
            if (slot != m_ConnectedSlot)
            {
                if (slot != null && !Semantics.CanLink(slot.Semantics))
                    throw new ArgumentException();

                if (m_ConnectedSlot != null)
                    m_ConnectedSlot.InnerRemoveOutputLink(this);

                m_ConnectedSlot = slot;
                VFXPropertySlot old = m_ValueRef;
                m_ValueRef = m_ConnectedSlot != null ? m_ConnectedSlot : null;
      
                if (m_ValueRef != old)
                {
                    if (slot != null)
                        slot.InnerAddOutputLink(this);
                    NotifyChange(Event.kLinkUpdated);

                    if (slot != null)
                        foreach (var child in m_Children)
                            child.UnlinkRecursively();

                    return true;
                }
            }

            return false;
        }

        public new VFXInputSlot GetChild(int index)
        {
            return (VFXInputSlot)m_Children[index];
        }

        public new VFXInputSlot Parent
        {
            get { return (VFXInputSlot)m_Parent; }
        }

        public override bool IsLinked()
        {
            return m_ConnectedSlot != null;
        }

        public override List<VFXPropertySlot> GetConnectedSlots()
        {
            List<VFXPropertySlot> connected = new List<VFXPropertySlot>();
            if (IsLinked())
                connected.Add(m_ConnectedSlot);
            return connected;
        }

        public void Unlink()
        {
            Link(null);
        }

        public override void UnlinkAll()
        {
            Unlink();
        }

        public override VFXPropertySlot CurrentValueRef
        {
            get { return m_ValueRef == null ? this : m_ValueRef; }
        }
      
        private VFXPropertySlot m_ValueRef;
        private VFXOutputSlot m_ConnectedSlot;
    }

    // Concrete implementation for output slot (can be linked to several input slot)
    public class VFXOutputSlot : VFXPropertySlot
    {
        public VFXOutputSlot() {}
        public VFXOutputSlot(VFXProperty desc)
        {
            Init<VFXOutputSlot>(null, desc); 
        }

        public override void PropagateChange(VFXPropertySlot.Event type)
        {
            foreach (var slot in m_ConnectedSlots)
                slot.NotifyChange(type);
        }

        // Called internally only!
        internal void InnerAddOutputLink(VFXInputSlot slot)
        {
            // Do we need to notify the output when a new slot is linked ?
            m_ConnectedSlots.Add(slot);
        }

        internal void InnerRemoveOutputLink(VFXInputSlot slot)
        {
            // Do we need to notify the output when a new slot is linked ?
            m_ConnectedSlots.Remove(slot);
        }

        public override VFXPropertySlot CurrentValueRef
        {
            get { return this; }
        }

        public new VFXOutputSlot GetChild(int index)
        {
            return (VFXOutputSlot)m_Children[index];
        }

        public new VFXOutputSlot Parent
        {
            get { return (VFXOutputSlot)m_Parent; }
        }

        public override bool IsLinked()
        {
            return m_ConnectedSlots.Count > 0;
        }

        public override List<VFXPropertySlot> GetConnectedSlots()
        {
            List<VFXPropertySlot> connected = new List<VFXPropertySlot>();
            foreach (var slot in m_ConnectedSlots)
                connected.Add(slot);
            return connected;
        }

        public void Link(VFXInputSlot slot)
        {
            if (slot == null)
                return;

            slot.Link(this); // This will call InnerUpdateOutputLink if needed
        }

        public void Unlink(VFXInputSlot slot)
        {
            if (slot == null)
                return;

            slot.Unlink();
        }

        public override void UnlinkAll()
        {
            foreach (var slot in m_ConnectedSlots.ToArray()) // must copy the list else we'll get an issue with iterator invalidation
                slot.Unlink();
            m_ConnectedSlots.Clear();
        }

        private List<VFXInputSlot> m_ConnectedSlots = new List<VFXInputSlot>();
    }
}
