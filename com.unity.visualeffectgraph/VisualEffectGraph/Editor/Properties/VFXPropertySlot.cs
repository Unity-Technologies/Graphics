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

        // Throw if incompatible or inexistant
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

        private VFXExpression m_OwnedValue;

        protected VFXPropertySlotObserver m_Observer; // Owner of the node. Can be a function/block...

        private VFXProperty m_Desc; // Contains semantic type and name for this value

        private VFXPropertySlot m_Parent;
        protected VFXPropertySlot[] m_Children = new VFXPropertySlot[0];
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
