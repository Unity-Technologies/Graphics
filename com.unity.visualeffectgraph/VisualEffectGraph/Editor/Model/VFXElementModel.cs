using UnityEngine;
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

    public abstract class VFXElementModelTyped<OwnerType, ChildrenType> : VFXElementModel
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
}
