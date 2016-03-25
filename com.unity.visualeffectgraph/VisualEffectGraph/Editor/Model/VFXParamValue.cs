using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public abstract class VFXParamValue
    {
        protected struct Binding
        {
            public VFXParamBindable m_Bindable;
            public int m_Index;

            public Binding(VFXParamBindable bindable, int index)
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

        protected VFXParam.Type m_Type;
        protected List<Binding> m_Bindings = new List<Binding>();

        public VFXParam.Type ValueType
        {
            get { return m_Type; }
        }

        // Create from VFXParam.Type
        public static VFXParamValue Create(VFXParam.Type type)
        {
            switch (type)
            {
                case VFXParam.Type.kTypeFloat: return new VFXParamValueFloat();
                case VFXParam.Type.kTypeFloat2: return new VFXParamValueFloat2();
                case VFXParam.Type.kTypeFloat3: return new VFXParamValueFloat3();
                case VFXParam.Type.kTypeFloat4: return new VFXParamValueFloat4();
                case VFXParam.Type.kTypeInt: return new VFXParamValueInt();
                case VFXParam.Type.kTypeUint: return new VFXParamValueUint();
                case VFXParam.Type.kTypeTexture2D: return new VFXParamValueTexture2D();
                case VFXParam.Type.kTypeTexture3D: return new VFXParamValueTexture3D();
                default:
                    throw new ArgumentException("Invalid parameter type");
            }
        }

        // Create from concrete type
        public static VFXParamValue Create<T>()
        {
            Type t = typeof(T);
            if (t == typeof(float))     return new VFXParamValueFloat();
            if (t == typeof(Vector2))   return new VFXParamValueFloat2();
            if (t == typeof(Vector3))   return new VFXParamValueFloat3();
            if (t == typeof(Vector4))   return new VFXParamValueFloat4();
            if (t == typeof(int))       return new VFXParamValueInt();
            if (t == typeof(uint))      return new VFXParamValueUint();
            if (t == typeof(Texture2D)) return new VFXParamValueTexture2D();
            if (t == typeof(Texture3D)) return new VFXParamValueTexture3D();

            throw new ArgumentException("Invalid parameter type");
        }

        public static VFXParamValue Create<T>(T value)
        {
            VFXParamValue v = Create<T>();
            v.SetValue(value);
            return v;
        }

        public abstract VFXParamValue Clone();
        public abstract void SetValue(VFXParamValue other);

        //TODO: Problem is that implicit cast between types will not work here !
        public T GetValue<T>()              { return ((VFXParamValue<T>)this).Value; }
        public void SetValue<T>(T value)    { ((VFXParamValue<T>)this).Value = value; }

        public void Bind(VFXParamBindable bindable, int index, bool reentrant = false)
        {
            Binding binding = new Binding(bindable, index);
            if (m_Bindings.IndexOf(binding) != -1) // Already bound
                return;

            m_Bindings.Add(binding);
            if (!reentrant)
                bindable.BindParam(this, index, true);
        }

        public void Unbind(VFXParamBindable bindable, int index, bool reentrant = false)
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
    }

    public abstract class VFXParamValue<T> : VFXParamValue
    {
        private T m_Value = default(T);

        public T Value
        {
            get { return m_Value; }
            set
            {
                if ((m_Value == null && value != null) || (m_Value != null && !m_Value.Equals(value)))
                {
                    VFXParamValue oldValue = Clone();
                    m_Value = value;
                    foreach (var binding in m_Bindings)
                        binding.m_Bindable.OnParamUpdated(binding.m_Index,oldValue);
                }
            }
        }

        public override VFXParamValue Clone()
        {
            VFXParamValue<T> param = (VFXParamValue<T>)MemberwiseClone();
            param.m_Bindings = new List<Binding>();
            return param;
        }

        public override void SetValue(VFXParamValue other)
        {
            SetValue<T>(other.GetValue<T>());
        }

        public override string ToString()
        {
            return m_Value != null ? m_Value.ToString() : "null value";
        }
    }

    public class VFXParamValueFloat : VFXParamValue<float>          { public VFXParamValueFloat()       { m_Type = VFXParam.Type.kTypeFloat; }}
    public class VFXParamValueFloat2 : VFXParamValue<Vector2>       { public VFXParamValueFloat2()      { m_Type = VFXParam.Type.kTypeFloat2; }}
    public class VFXParamValueFloat3 : VFXParamValue<Vector3>       { public VFXParamValueFloat3()      { m_Type = VFXParam.Type.kTypeFloat3; }}
    public class VFXParamValueFloat4 : VFXParamValue<Vector4>       { public VFXParamValueFloat4()      { m_Type = VFXParam.Type.kTypeFloat4; }}
    public class VFXParamValueInt : VFXParamValue<int>              { public VFXParamValueInt()         { m_Type = VFXParam.Type.kTypeInt; }}
    public class VFXParamValueUint : VFXParamValue<uint>            { public VFXParamValueUint()        { m_Type = VFXParam.Type.kTypeUint; }}
    public class VFXParamValueTexture2D : VFXParamValue<Texture2D>  { public VFXParamValueTexture2D()   { m_Type = VFXParam.Type.kTypeTexture2D; }}
    public class VFXParamValueTexture3D : VFXParamValue<Texture3D>  { public VFXParamValueTexture3D()   { m_Type = VFXParam.Type.kTypeTexture3D; }}
}
