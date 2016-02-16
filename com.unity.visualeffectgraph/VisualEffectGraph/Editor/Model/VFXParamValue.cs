using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public abstract class VFXParamValue
    {
        protected VFXParam.Type m_Type;
        protected List<VFXBlockModel> m_BoundModels = new List<VFXBlockModel>();

        public VFXParam.Type ValueType
        {
            get { return m_Type; }
        }

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

        //TODO: Problem is that implicit cast between types will not work here !
        public T GetValue<T>()              { return ((VFXParamValue<T>)this).Value; }
        public void SetValue<T>(T value)    { ((VFXParamValue<T>)this).Value = value; }
    }

    public abstract class VFXParamValue<T> : VFXParamValue
    {
        private T m_Value = default(T);

        public void test() {}

        public T Value
        {
            get { return m_Value; }
            set
            {
                if (!m_Value.Equals(value))
                {
                    m_Value = value;
                    foreach (var model in m_BoundModels)
                        model.Invalidate(VFXElementModel.InvalidationCause.kParamChanged);
                }
            }
        }
    }

    public class VFXParamValueFloat : VFXParamValue<float>          { public VFXParamValueFloat()      { m_Type = VFXParam.Type.kTypeFloat; }}
    public class VFXParamValueFloat2 : VFXParamValue<Vector2>       { public VFXParamValueFloat2()     { m_Type = VFXParam.Type.kTypeFloat2; }}
    public class VFXParamValueFloat3 : VFXParamValue<Vector3>       { public VFXParamValueFloat3()     { m_Type = VFXParam.Type.kTypeFloat3; }}
    public class VFXParamValueFloat4 : VFXParamValue<Vector4>       { public VFXParamValueFloat4()     { m_Type = VFXParam.Type.kTypeFloat4; }}
    public class VFXParamValueInt : VFXParamValue<int>              { public VFXParamValueInt()        { m_Type = VFXParam.Type.kTypeInt; }}
    public class VFXParamValueUint : VFXParamValue<uint>            { public VFXParamValueUint()       { m_Type = VFXParam.Type.kTypeUint; }}
    public class VFXParamValueTexture2D : VFXParamValue<Texture2D>  { public VFXParamValueTexture2D()  { m_Type = VFXParam.Type.kTypeTexture2D; }}
    public class VFXParamValueTexture3D : VFXParamValue<Texture3D>  { public VFXParamValueTexture3D() { m_Type = VFXParam.Type.kTypeTexture3D; }}
}