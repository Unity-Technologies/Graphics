using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    [Serializable]
    abstract class VFXModel : ScriptableObject, ISerializationCallbackReceiver
    {
        public enum InvalidationCause
        {
            kStructureChanged,  // Model structure (hierarchy) has changed
            kParamChanged,      // Some parameter values have changed
            kDataChanged,       // Data layout have changed
            kUIChanged,         // UI stuff has changed
        }

        public virtual string name  { get { return string.Empty; } }
        public Guid id              { get { return m_Id; } }

        public delegate void InvalidateEvent(VFXModel model, InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        protected VFXModel()
        {
            m_Id = Guid.NewGuid();
        }

        public void OnEnable()
        {
            if (m_Children == null)
                m_Children = new List<VFXModel>();
            else
                m_Children.RemoveAll(c => c == null); // Remove bad references if any
        }

        public virtual void CollectDependencies(HashSet<UnityEngine.Object> objs)
        {
            foreach (var child in children)
            {
                objs.Add(child);
                child.CollectDependencies(objs);
            }
        }

        public virtual T Clone<T>() where T : VFXModel
        {
            T clone = (T)Instantiate(this);
            clone.m_Parent = null;
            return clone;
        }

        protected virtual void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
            {
                onInvalidateDelegate(model, cause);
            }
        }
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

                //AssetDatabase.AddObjectToAsset(model, this);

                m_Children.Insert(realIndex, model);
                model.m_Parent = this;
                model.OnAdded();

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public void RemoveChild(VFXModel model, bool notify = true)
        {
            if (model.m_Parent != this)
                return;

            model.OnRemoved();
            m_Children.Remove(model);
            model.m_Parent = null;

            //AssetDatabase.AddObjectToAsset(model, (UnityEngine.Object)null);
            
            if (notify)
                Invalidate(InvalidationCause.kStructureChanged);     
        }

        public void RemoveAllChildren(bool notify = true)
        {
            while (m_Children.Count > 0)
                RemoveChild(m_Children[m_Children.Count - 1], notify);
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

        public IEnumerable<VFXModel> children
        {
            get { return m_Children; }
        }

        public IEnumerable<VFXModel> GetChildren()
        {
            return m_Children;
        }

        public VFXModel GetChild(int index)
        {
            return m_Children[index];
        }

        public VFXModel this[int index]
        {
            get { return GetChild(index); }
        }

        public Vector2 position
        {
            get { return m_UIPosition; }
            set
            {
                if (m_UIPosition != value)
                {
                    m_UIPosition = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool collapsed
        {
            get { return m_UICollapsed; }
            set
            {
                if (m_UICollapsed != value)
                {
                    m_UICollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
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
            Invalidate(this,cause);
        }

        private void Invalidate(VFXModel model,InvalidationCause cause)
        {
            OnInvalidate(model,cause);
            if (m_Parent != null)
                m_Parent.Invalidate(model,cause);
        }

        public virtual void OnBeforeSerialize()
        {
           /* m_SerializableId = m_Id.ToString();
            m_SerializableChildren = SerializationHelper.Serialize<VFXModel>(m_Children);*/
        }

        public virtual void OnAfterDeserialize()
        {
            /*if (!String.IsNullOrEmpty(m_SerializableId))
                m_Id = new Guid(m_SerializableId);
            else
                m_Id = Guid.NewGuid();
            m_Children = SerializationHelper.Deserialize<VFXModel>(m_SerializableChildren, null);
            foreach (var child in m_Children)
                child.m_Parent = this;
            m_SerializableChildren = null; // No need to keep it
            */
        }
      
        private Guid m_Id;

        [SerializeField]
        private string m_SerializableId;

        [SerializeField]
        protected VFXModel m_Parent = null;

        [SerializeField]
        protected List<VFXModel> m_Children;

      /*  [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableChildren = null;*/

        [SerializeField]
        private Vector2 m_UIPosition;

        [SerializeField]
        private bool m_UICollapsed;
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

        public new ChildrenType this[int index]
        {
            get { return GetChild(index); }
        }

        public new IEnumerable<ChildrenType> children
        {
            get { return m_Children.Cast<ChildrenType>(); }
        }

        public new IEnumerable<ChildrenType> GetChildren()
        {
            return base.GetChildren().Cast<ChildrenType>();
        }
    }
}
