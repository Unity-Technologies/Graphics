using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public abstract class VFXElementModel
    {
        public enum InvalidationCause
        {
            kModelChanged,
            kParamChanged,
        }

        public void AddChild(VFXElementModel child, int index = -1, bool notify = true)
        {
            if (!CanAddChild(child, index))
                throw new ArgumentException("Cannot attach " + child + " to " + this);

            child.Detach(notify && child.m_Owner != this); // Dont notify if the owner is already this to avoid double invalidation

            int realIndex = index == -1 ? m_Children.Count : index;
            m_Children.Insert(realIndex, child);
            child.m_Owner = this;

            if (notify)
                Invalidate(InvalidationCause.kModelChanged);

            //Debug.Log("Attach " + child + " to " + this + " at " + realIndex);
        }

        public void Remove(VFXElementModel child, bool notify = true)
        {
            if (child.m_Owner != this)
                return;

            m_Children.Remove(child);
            child.m_Owner = null;

            if (notify)
                Invalidate(InvalidationCause.kModelChanged);

            //Debug.Log("Detach " + child + " to " + this);
        }

        public void Attach(VFXElementModel owner, bool notify = true)
        {
            if (owner == null)
                throw new ArgumentNullException();

            owner.AddChild(this, -1, notify);
        }

        public void Detach(bool notify = true)
        {
            if (m_Owner == null)
                return;

            m_Owner.Remove(this, notify);
        }

        public abstract bool CanAddChild(VFXElementModel element, int index);
        public abstract void Invalidate(InvalidationCause cause);

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXElementModel element)
        {
            return m_Children.IndexOf(element);
        }

        protected VFXElementModel m_Owner;
        protected List<VFXElementModel> m_Children = new List<VFXElementModel>();
    }

    public abstract class VFXElementModel<OwnerType, ChildrenType> : VFXElementModel
        where OwnerType : VFXElementModel
        where ChildrenType : VFXElementModel
    {
        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return index >= -1 && index <= m_Children.Count && element is ChildrenType;
        }

        public ChildrenType GetChild(int index)
        {
            return m_Children[index] as ChildrenType;
        }

        public OwnerType GetOwner()
        {
            return m_Owner as OwnerType;
        }
    }

    public abstract class VFXModelWithSlots<OwnerType, ChildrenType> : VFXElementModel<OwnerType, ChildrenType>, VFXPropertySlotObserver
        where OwnerType : VFXElementModel
        where ChildrenType : VFXElementModel
    {
        protected void InitSlots(VFXProperty[] desc)
        {
            if (m_Slots != null)
                foreach (var slot in m_Slots)
                    slot.UnlinkAll();

            if (desc == null)
                m_Slots = null;
            else
            {
                int nbSlots = desc.Length;
                m_Slots = new VFXInputSlot[nbSlots];
                for (int i = 0; i < nbSlots; ++i)
                    m_Slots[i] = new VFXInputSlot(desc[i],this);
            }
        }

        public void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            switch (type)
            {
                case VFXPropertySlot.Event.kLinkUpdated:
                    Invalidate(InvalidationCause.kModelChanged);
                    break;
                case VFXPropertySlot.Event.kValueUpdated:
                    Invalidate(InvalidationCause.kParamChanged);
                    break;
            }
        }

        public VFXInputSlot GetSlot(int index)
        {
            return m_Slots[index];
        }

        public int GetNbSlots()
        {
            return m_Slots == null ? 0 : m_Slots.Length;
        }

        private VFXInputSlot[] m_Slots;
    }
}
