using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    [Serializable]
    abstract class VFXModel : ISerializationCallbackReceiver
    {
        public enum InvalidationCause
        {
            kModelChanged,  // Model layout has changed
            kParamChanged,  // Some parameter values have changed
            kDataChanged,   // Data layout have changed
            kUIChanged,     // UI stuff has changed
        }

        protected virtual void OnInvalidate(InvalidationCause cause) {}
        protected virtual void OnAdded() {}
        protected virtual void OnRemoved() {}

        public abstract bool AcceptChild(VFXModel model, int index = -1);

        public void AddChild(VFXModel model, int index = -1, bool notify = true)
        {
            int realIndex = index == -1 ? m_Children.Count : index;
            if (model.m_Parent != this || realIndex != GetIndex(model))
            {
                if (!AcceptChild(model, index))
                    throw new ArgumentException("Cannot attach " + model + " to " + this);

                model.Detach(notify && model.m_Parent != this); // Dont notify if the owner is already this to avoid double invalidation

                realIndex = index == -1 ? m_Children.Count : index; // Recompute as the child may have been removed

                m_Children.Insert(realIndex, model);
                model.m_Parent = this;
                model.OnAdded();

                if (notify)
                    Invalidate(InvalidationCause.kModelChanged);
            }
        }

        public void RemoveChild(VFXModel model, bool notify = true)
        {
            if (model.m_Parent != this)
                return;

            model.OnRemoved();
            m_Children.Remove(model);
            model.m_Parent = null;

            if (notify)
                Invalidate(InvalidationCause.kModelChanged);
        }

        public VFXModel GetParent()
        {
            return m_Parent;
        }

        public void Attach(VFXModel parent, bool notify = true)
        {
            if (parent == null)
                throw new ArgumentNullException();

            parent.AddChild(this, -1, notify);
        }

        public void Detach(bool notify = true)
        {
            if (m_Parent == null)
                return;

            m_Parent.RemoveChild(this, notify);
        }

        public IEnumerable<VFXModel> GetChildren()
        {
            return m_Children;
        }

        public VFXModel GetChild(int index)
        {
            return m_Children[index];
        }        

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXModel child)
        {
            return m_Children.IndexOf(child);
        }

        public void Invalidate(InvalidationCause cause)
        {
            OnInvalidate(cause);
            if (m_Parent != null)
                m_Parent.Invalidate(cause);
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableChildren = SerializationHelper.Serialize<VFXModel>(m_Children);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Children = SerializationHelper.Deserialize<VFXModel>(m_SerializableChildren, null);
            foreach (var child in m_Children)
                child.m_Parent = this;
            m_SerializableChildren = null; // No need to keep it
        }

        protected VFXModel m_Parent = null;
        protected List<VFXModel> m_Children = new List<VFXModel>();

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableChildren = null; 
    }

    abstract class VFXModel<ParentType, ChildrenType> : VFXModel
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && model is ChildrenType;
        }

        public new ParentType GetParent()
        {
            return (ParentType)m_Parent;
        }

        public new ChildrenType GetChild(int index)
        {
            return (ChildrenType)m_Children[index];
        }
    }
}
