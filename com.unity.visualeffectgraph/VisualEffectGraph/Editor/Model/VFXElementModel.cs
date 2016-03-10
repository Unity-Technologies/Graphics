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

    public abstract class VFXParamBindableModel<OwnerType, ChildrenType> : VFXElementModel<OwnerType, ChildrenType>, VFXParamBindable
        where OwnerType : VFXElementModel
        where ChildrenType : VFXElementModel
    {
        protected void InitParamValues(VFXParam[] desc)
        {
            if (m_ParamValues != null)
                foreach (var paramValue in m_ParamValues)
                    paramValue.UnbindAll();

            if (desc == null)
                m_ParamValues = null;
            else
            {
                int nbParams = desc.Length;
                m_ParamValues = new VFXParamValue[nbParams];
                for (int i = 0; i < nbParams; ++i)
                    m_ParamValues[i] = VFXParamValue.Create(desc[i].m_Type);
            }
        }

        protected void BindParam(VFXParamValue value, int index, VFXParam[] desc,bool reentrant)
        {
            if (index < 0 || index >= desc.Length || value.ValueType != desc[index].m_Type)
                throw new ArgumentException();

            if (!reentrant)
            {
                if (m_ParamValues[index] != null)
                    m_ParamValues[index].Unbind(this, index, true);
                value.Bind(this, index, true);
            }

            m_ParamValues[index] = value;
            Invalidate(InvalidationCause.kModelChanged);
        }

        protected void UnbindParam(int index, VFXParam[] desc, bool reentrant)
        {
            if (index < 0 || index >= desc.Length)
                throw new ArgumentException();

            if (!reentrant && m_ParamValues[index] != null)
                m_ParamValues[index].Unbind(this, index, true);

            m_ParamValues[index] = VFXParamValue.Create(desc[index].m_Type);
            Invalidate(InvalidationCause.kModelChanged);
        }

        public abstract void BindParam(VFXParamValue param, int index, bool reentrant = false);
        public abstract void UnbindParam(int index, bool reentrant = false);

        public void OnParamUpdated(int index)
        {
            Invalidate(InvalidationCause.kParamChanged);
        }

        public VFXParamValue GetParamValue(int index)
        {
            return m_ParamValues[index];
        }

        public int GetNbParamValues()
        {
            return m_ParamValues == null ? 0 : m_ParamValues.Length;
        }

        private VFXParamValue[] m_ParamValues;
    }

    public interface VFXParamBindable
    {
        void BindParam(VFXParamValue param, int index, bool reentrant = false);
        void UnbindParam(int index, bool reentrant = false);
        void OnParamUpdated(int index);
        VFXParamValue GetParamValue(int index);
        int GetNbParamValues();
    }
}
