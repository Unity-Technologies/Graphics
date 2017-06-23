using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Profiling;

namespace UnityEditor.VFX
{
    [Serializable]
    abstract class VFXModel : ScriptableObject
    {
        public enum InvalidationCause
        {
            kStructureChanged,      // Model structure (hierarchy) has changed
            kParamChanged,          // Some parameter values have changed
            kParamPropagated,       // Some parameter values have change and was propagated from the parents
            kParamExpanded,         // Some parameter values have beed expanded or retracted
            kSettingChanged,        // A setting value has changed
            kConnectionChanged,     // Connection have changed
            kExpressionInvalidated, // No direct change to the model but a change in connection was propagated from the parents
            kExpressionGraphChanged,// Expression graph must be recomputed
            kUIChanged,             // UI stuff has changed
        }

        public new virtual string name  { get { return string.Empty; } }
        public Guid id              { get { return m_Id; } }

        public int m_OnEnabledCount = 0;

        public string DebugName
        {
            get { return string.Format("{0} {1}", GetType().Name, id); }
        }

        public delegate void InvalidateEvent(VFXModel model, InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        protected VFXModel()
        {
            m_Id = Guid.NewGuid();
        }

        public virtual void OnEnable()
        {
            if (m_Children == null)
                m_Children = new List<VFXModel>();
            else
            {
                int nbRemoved = m_Children.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} child(ren) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
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

            clone.m_Children.Clear();
            foreach (var child in children)
            {
                var cloneChild = child.Clone<VFXModel>();
                clone.m_Children.Add(cloneChild);
                cloneChild.m_Parent = clone;
            }

            clone.m_Parent = null;
            return clone;
        }

        protected virtual void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
            {
                Profiler.BeginSample("OnInvalidateDelegate");
                try
                {
                    onInvalidateDelegate(model, cause);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
        }

        protected virtual void OnAdded() {}
        protected virtual void OnRemoved() {}

        public virtual bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

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
            string sampleName = GetType().Name + "-" + name + "-" + cause + "-Invalidate";
            Profiler.BeginSample(sampleName);
            try
            {
                Invalidate(this, cause);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected virtual void Invalidate(VFXModel model, InvalidationCause cause)
        {
            OnInvalidate(model, cause);
            if (m_Parent != null)
                m_Parent.Invalidate(model, cause);
        }

        private Guid m_Id; // TODO Not used

        [SerializeField]
        protected VFXModel m_Parent = null;

        [SerializeField]
        protected List<VFXModel> m_Children;

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
