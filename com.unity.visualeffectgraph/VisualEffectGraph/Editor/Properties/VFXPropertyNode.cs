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

    public interface VFXPropertyNode {}

    public sealed class VFXPropertyValue : VFXPropertyNode, VFXPropertyBindable
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
                    binding.m_Bindable.OnParamUpdated(binding.m_Index, /*oldValue*/ null); // TODO
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
    }
}
