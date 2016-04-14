using UnityEngine;
using UnityEditor; // Shouldnt be included!
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.VFX
{
    public interface VFXPropertyBindable
    {
        void BindParam(VFXPropertyNode param, int index, bool reentrant = false);
        void UnbindParam(int index, bool reentrant = false);
        void OnParamUpdated(int index, VFXPropertyNode oldValue);
        VFXPropertyNode GetParamValue(int index);
        int GetNbParamValues();
    }

    public interface VFXPropertyNodeOwner
    {
        OnUpdated(VFXPropertyNode node)
    }

    public interface VFXPropertyNode {}

    /*public sealed class VFXPropertyValue : VFXPropertyNode, VFXPropertyBindable
    {
        protected struct Binding
        {
            public VFXPropertyBindable m_Bindable;
            public int m_Index;

            public Binding(VFXPropertyBindable bindable, int index)
            {
                m_Bindable = bindable;
                m_Index = index;
            }

            public override bool Equals(object obj)
            {
                if (obj is Binding)
                {
                    Binding typedObj = (Binding)obj;
                    return m_Bindable == typedObj.m_Bindable && m_Index == typedObj.m_Index;
                }
                return false;
            }
        }

        public VFXShaderValueType ValueType { get { return m_Value.ValueType; }}
        public T GetValue<T>() { return m_Value.Get<T>(); }
        public void SetValue<T>(T value) 
        {
            var newValue = m_Value.Clone();
            newValue.Set<T>(value);
            m_Type.Constrain(newValue);
            if (parent != null)
                parent.m_type.Constrain(parent);
            if (m_Value.Set(newValue))
            {
                foreach (var binding in m_Bindings)
                    binding.m_Bindable.OnParamUpdated(binding.m_Index, null); // TODO
            }        
        }

        public void Bind(VFXPropertyBindable bindable, int index, bool reentrant = false)
        {
            Binding binding = new Binding(bindable, index);
            if (m_Bindings.IndexOf(binding) != -1) // Already bound
                return;

            m_Bindings.Add(binding);
            if (!reentrant)
                bindable.BindParam(this, index, true);
        }

        public void Unbind(VFXPropertyBindable bindable, int index, bool reentrant = false)
        {
            Binding binding = new Binding(bindable, index);
            if (m_Bindings.Remove(binding) && reentrant)
                bindable.UnbindParam(index, true);
        }

        public void UnbindAll()
        {
            foreach (var binding in m_Bindings)
                binding.m_Bindable.UnbindParam(binding.m_Index);
        }

        public bool IsBound()
        {
            return m_Bindings.Count > 0;
        }

        private List<Binding> m_Bindings = new List<Binding>();
        private VFXShaderValue m_Value;
        private VFXPropertyType m_Type;
    }*/

    class VFXPropertyValue : VFXPropertyNode
    {
        private VFXPropertyNodeOwner m_Owner;

        private VFXPropertyValue m_Parent;
        private VFXPropertyValue[] m_Children;

        private VFXProperty m_Desc;
        private VFXValue m_Value;

        // Used during processing of values
        private bool m_BeingProcessed = false;
        private VFXValue m_OldValue; // Keep old value for propagation
        private bool m_Constraining = false; // Currently in constrained mode (needs that to avoid infinite pingponging)

        private VFXPropertyValue m_Input;
        private List<VFXPropertyValue> m_Outputs;

        private void Constrain()
        {
            if (!m_Constraining) // If not already constraining
            {
                m_Constraining = true;
                m_Desc.m_Type.Constrain(this); // Constrained from bottom to top, parent is supposed to keep children constraints !
                m_Parent.Constrain();
                m_Constraining = false;
            }
        }

        private VFXPropertyValue GetRoot()
        {
            return m_Parent != null ? m_Parent.GetRoot() : this;
        }

        private void MarkBeingProcessedRecursively(bool beginProcessed)
        {
            m_BeingProcessed = beginProcessed;
            foreach (var child in m_Children)
                MarkBeingProcessedRecursively(beginProcessed);
        }

        // Useful to be called when a series of Set is performed to avoid notify outputs/owner after each set
        public bool BeginUpdateProcess()
        {
            if (m_BeingProcessed)
                return false;

            GetRoot().MarkBeingProcessedRecursively(true);
            return true;
        }

        // Will trigger propagation if anything has changed
        public void EndUpdateProcess()
        {
            var root = GetRoot();
            root.MarkBeingProcessedRecursively(false);
            root.PropagateChanges();
        }

        private bool PropagateChanges()
        {
            bool dirty = m_OldValue != null && !m_OldValue.Equals(m_Value);
            m_OldValue = null;

            foreach (var child in m_Children)
                dirty |= child.PropagateChanges();

            if (dirty)
            {
                if (m_Owner != null)
                    m_Owner.OnUpdated(this);
                foreach (var output in m_Outputs)
                    output.Refresh();
            }

            return dirty;
        }

        public void Set<T>(T val)
        {
            var value = VFXValue.Create<T>(); // TODO Needs a pool for shader value
            Set(value);
        }

        public T Get<T>()
        {
            return GetValue().Get<T>();
        }

        public void SetValue(VFXValue value)
        {
            if (!m_Value.Equals(value)) // Only if value has changed
            {
                bool initialChange = BeginUpdateProcess(); // If this is the initial change, this object is responsible to propagate the changes later on

                if (m_Value != null && m_OldValue == null) // If value not already cached
                    m_OldValue = m_Value.Clone(); // TODO Needs a pool for shader value

                m_Value.SetValue(value);
                Constrain(); // This may invalidate other values, hence the old value caching       

                if (initialChange) // Now trigger refresh for linked nodes from the initial change
                    EndUpdateProcess();
            }
        }

        public VFXValue GetValue()
        {
            return m_Value;
        }

        public void Refresh()
        {
            if (m_Input != null)
            {
                BeginUpdateProcess();
                m_Desc.m_Type.Transform(this, m_Input); // This is not supposed to throw as the compatibility was ensured when linking
                EndUpdateProcess();
            }
        }

        public void SetInput(VFXPropertyValue link)
        {
            if (m_Input != link && link.m_Desc.CanTransform(this))
            {
                if (link != null)
                    link.m_Outputs.Remove(this);

                m_Input = this;
                link.m_Outputs.Add(this);
                Refresh();
            }
        }
    }
}
