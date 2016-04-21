using UnityEngine;
using UnityEditor; // Shouldnt be included!
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
        public VFXNamedValue(string name, VFXValue value)
        {
            m_Name = name;
            m_Value = value;
        }

        public string m_Name;
        public VFXValue m_Value;
    }

    public abstract class VFXPropertySlot
    {
        public enum Event
        {
            kLinkUpdated,
            kValueUpdated,
        }

        public VFXPropertySlot() {}
        public VFXPropertySlot(VFXProperty desc,VFXPropertySlotObserver observer = null)
        {
            Init(null,desc,observer);     
        }

        private void Init(VFXPropertySlot parent,VFXProperty desc,VFXPropertySlotObserver observer)
        {
            m_Parent = parent;
            m_Observer = observer;
            m_Desc = desc;
            Semantics.CreateValue(this);
 
            m_FullName = desc.m_Name;
            if (m_Parent != null)
                m_FullName = m_Parent.m_FullName + "_" + desc.m_Name;
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
                    child.Init(this,children[i],m_Observer);
                    m_Children[i] = child;
                }
            }

            SetDefault();
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

        // Throw if incompatible or inexistent
        public void SetValue<T>(T t)
        {
            if (m_OwnedValue.Set(t))
                NotifyChange(Event.kValueUpdated);
        }

        public T GetValue<T>()
        {
            return m_OwnedValue.Get<T>();
        }

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
            set
            {
                CurrentValueRef.Value = value;
            }
            get { return CurrentValueRef.Value; }
        }

        public void NotifyChange(Event type)
        {
            // Invalidate expression cache
            m_OwnedValue.Invalidate();

            if (m_Observer != null)
                m_Observer.OnSlotEvent(type,this);
            PropagateChange(type);
        }

        public virtual void PropagateChange(Event type) {}

        public abstract VFXPropertySlot CurrentValueRef { get; }

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

        public void FlattenValues<T>(List<T> values) where T : VFXExpression
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            values.Add(refSlot.Value as T); 
            foreach (var child in refSlot.m_Children)
                child.FlattenValues(values);
        }

        public void FlattenOwnedValues<T>(List<T> values) where T : VFXExpression
        {
            values.Add(Value as T);
            foreach (var child in m_Children)
                child.FlattenOwnedValues(values);
        }

        // Collect all values in the slot hierarchy with the name used in the shader
        // Called from the model compiler
        public void CollectNamedValues(List<VFXNamedValue> values)
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            VFXExpression refValue = refSlot.Value;
            
            if (refValue != null)
            {
                VFXValue reduced = refValue.Reduce() as VFXValue;
                if (reduced != null)
                    values.Add(new VFXNamedValue(m_FullName,reduced));
            }

            foreach (var child in refSlot.m_Children)
                child.CollectNamedValues(values);
        }

        private VFXExpression m_OwnedValue;

        protected VFXPropertySlotObserver m_Observer; // Owner of the node. Can be a function/block...

        private VFXProperty m_Desc; // Contains semantic type and name for this value

        protected VFXPropertySlot m_Parent;
        protected VFXPropertySlot[] m_Children = new VFXPropertySlot[0];

        private string m_FullName; // name in the slot hierarchy. In the form: parent0_[...]_parentN_propertyName
    }

    public class VFXInputSlot : VFXPropertySlot
    {
        public VFXInputSlot() {}
        public VFXInputSlot(VFXProperty desc,VFXPropertySlotObserver owner = null)
            : base(desc,owner)
        {
            CreateChildren<VFXInputSlot>();   
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

    public class VFXOutputSlot : VFXPropertySlot
    {
        public VFXOutputSlot() {}
        public VFXOutputSlot(VFXProperty desc,VFXPropertySlotObserver owner = null)
            : base(desc,owner)
        {
            CreateChildren<VFXOutputSlot>();
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
            foreach (var slot in m_ConnectedSlots)
                slot.Unlink();
            m_ConnectedSlots.Clear();
        }

        private List<VFXInputSlot> m_ConnectedSlots = new List<VFXInputSlot>();
    }
}
