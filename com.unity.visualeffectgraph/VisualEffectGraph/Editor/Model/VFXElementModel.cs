using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public interface VFXUIDataHolder
    {
        void UpdateCollapsed(bool collapsed);
        void UpdatePosition(Vector2 position);
    }

    public interface VFXModelHolder
    {
        VFXElementModel GetAbstractModel();
    }

    public abstract class VFXElementModel
    {
        public enum InvalidationCause
        {
            kModelChanged,  // Model layout has changed
            kParamChanged,  // Some parameter values have changed
            kDataChanged,   // Data layout have changed
            kUIChanged,     // UI stuff has changed
        }

        public void AddChild(VFXElementModel child, int index = -1, bool notify = true)
        {
            int realIndex = index == -1 ? m_Children.Count : index;
            if (child.m_Owner != this || realIndex != GetIndex(child))
            {
                if (!CanAddChild(child, index))
                    throw new ArgumentException("Cannot attach " + child + " to " + this);

                child.Detach(notify && child.m_Owner != this); // Dont notify if the owner is already this to avoid double invalidation

                realIndex = index == -1 ? m_Children.Count : index; // Recompute as the child may have been removed
                m_Children.Insert(realIndex, child);
                child.m_Owner = this;

                if (notify)
                    Invalidate(InvalidationCause.kModelChanged);
            }

            //Debug.Log("Attach " + child + " to " + this + " at " + realIndex);
        }

        protected virtual void OnRemove() {}
        public void Remove(VFXElementModel child, bool notify = true)
        {
            if (child.m_Owner != this)
                return;

            child.OnRemove();
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

        public abstract bool CanAddChild(VFXElementModel element, int index = -1);
        public void Invalidate(InvalidationCause cause)
        {
            InnerInvalidate(cause);
            if (m_Owner != null)
                m_Owner.Invalidate(cause);
        }

        protected virtual void InnerInvalidate(InvalidationCause cause) {}

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXElementModel element)
        {
            return m_Children.IndexOf(element);
        }

        public VFXElementModel GetChild(int index)
        {
            return m_Children[index];
        }

        public VFXElementModel GetOwner()
        {
            return m_Owner;
        }

        protected VFXElementModel m_Owner;
        protected List<VFXElementModel> m_Children = new List<VFXElementModel>();
    }

    public abstract class VFXElementModel<OwnerType, ChildrenType> : VFXElementModel
        where OwnerType : VFXElementModel
        where ChildrenType : VFXElementModel
    {
        public override bool CanAddChild(VFXElementModel element, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && element is ChildrenType;
        }

        public new ChildrenType GetChild(int index)
        {
            return m_Children[index] as ChildrenType;
        }

        public new OwnerType GetOwner()
        {
            return m_Owner as OwnerType;
        }
    }

    public abstract class VFXModelWithSlots<OwnerType, ChildrenType> : VFXElementModel<OwnerType, ChildrenType>, VFXPropertySlotObserver
        where OwnerType : VFXElementModel
        where ChildrenType : VFXElementModel
    {
        protected void InitSlots(VFXProperty[] inputDesc,VFXProperty[] outputDesc)
        {
            // Input
            if (m_InputSlots != null)
                foreach (var slot in m_InputSlots)
                    slot.UnlinkAll();

            if (inputDesc == null)
                m_InputSlots = null;
            else
            {
                int nbSlots = inputDesc.Length;
                m_InputSlots = new VFXInputSlot[nbSlots];
                for (int i = 0; i < nbSlots; ++i)
                {
                    m_InputSlots[i] = new VFXInputSlot(inputDesc[i]);
                    m_InputSlots[i].AddObserver(this, true);
                }
            }

            // Output
            if (m_OutputSlots != null)
                foreach (var slot in m_OutputSlots)
                    slot.UnlinkAll();

            if (outputDesc == null)
                m_OutputSlots = null;
            else
            {
                int nbSlots = outputDesc.Length;
                m_OutputSlots = new VFXOutputSlot[nbSlots];
                for (int i = 0; i < nbSlots; ++i)
                {
                    m_OutputSlots[i] = new VFXOutputSlot(outputDesc[i]);
                    m_OutputSlots[i].AddObserver(this, true);
                }
            }
        }

        public virtual void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
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

        public VFXInputSlot GetInputSlot(int index)
        {
            return m_InputSlots[index];
        }

        public VFXOutputSlot GetOutputSlot(int index)
        {
            return m_OutputSlots[index];
        }

        public int GetNbInputSlots()
        {
            return m_InputSlots == null ? 0 : m_InputSlots.Length;
        }

        public int GetNbOutputSlots()
        {
            return m_OutputSlots == null ? 0 : m_OutputSlots.Length;
        }

        private VFXInputSlot[]  m_InputSlots;
        private VFXOutputSlot[] m_OutputSlots;
    }
}
