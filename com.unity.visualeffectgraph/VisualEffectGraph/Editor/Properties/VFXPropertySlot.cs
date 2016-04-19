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

        // Explicit get child with cast as there is no covariance on return type in c#!
        public T GetChild<T>(int index) where T : VFXPropertySlot
        {
            return (T)m_Children[index];
        }

        public VFXPropertySlot Parent
        {
            get { return m_Parent; }
        }

        // Throw if incompatible or inexistant
        public void SetValue<T>(T t)
        {
            m_OwnedValue.Set(t);
            NotifyChange(Event.kValueUpdated);
        }

        public VFXExpression Value
        {
            set
            {
                m_OwnedValue = value;
                NotifyChange(Event.kValueUpdated);
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

        public VFXPropertyTypeSemantics Semantics
        {
            get { return m_Desc.m_Type; }
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

                m_ConnectedSlot = slot;
                VFXPropertySlot old = m_ValueRef;
                m_ValueRef = m_ConnectedSlot != null ? m_ConnectedSlot : null;
      
                if (m_ValueRef != old)
                {
                    if (slot != null)
                        slot.InnerUpdateOutputLink(this);
                    NotifyChange(Event.kLinkUpdated);
                    return true;
                }
            }

            return false;
        }

        public void Unlink()
        {
            Link(null);
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
        internal void InnerUpdateOutputLink(VFXInputSlot slot)
        {
            // Do we need to notify the output when a new slot is linked ?
            m_ConnectedSlots.Add(slot);
        }

        public override VFXPropertySlot CurrentValueRef
        {
            get { return this; }
        }

        public void Link(VFXInputSlot slot)
        {
            slot.Link(this); // This will call InnerUpdateOutputLink if needed
        }

        public void Unlink(VFXInputSlot slot)
        {
            if (slot == null)
                return;

            if (m_ConnectedSlots.Remove(slot))
                slot.Unlink();
        } 

        private List<VFXInputSlot> m_ConnectedSlots = new List<VFXInputSlot>();
    }
}
