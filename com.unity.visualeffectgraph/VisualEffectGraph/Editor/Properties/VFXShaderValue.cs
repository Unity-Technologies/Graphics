using UnityEngine;

namespace UnityEngine.Experimental.VFX
{
    public enum VFXShaderValueType
    {
        kNone,
        kFloat,
        kFloat2,
        kFloat3,
        kFloat4,
        kInt,
        kUint,
        kTexture2D,
        kTexture3D,
        //...
        // Curve
        // Gradient
    }

    public abstract class VFXShaderValue
    {
        public abstract VFXShaderValue Clone();
        public abstract bool SetDefault();

        public T Get<T>() { return ((VFXShaderValue<T>)this).GetValue(); }
        public bool Set<T>(T value) { return ((VFXShaderValue<T>)this).SetValue(value); }

        public abstract bool SetValue(VFXShaderValue other);

        public virtual VFXShaderValueType ValueType { get { return VFXShaderValueType.kNone; }}
    }

    abstract class VFXShaderValue<T> : VFXShaderValue
    {
        public override VFXShaderValue Clone()
        {
            return  (VFXShaderValue<T>)MemberwiseClone();
        }

        public T GetValue() { return m_Value; }
        public bool SetValue(T t)
        {
            if ((m_Value == null && t != null) || (m_Value != null && !m_Value.Equals(t)))
            {
                m_Value = t;
                return true;
            }
            return false;
        }

        public override bool SetValue(VFXShaderValue other)
        {
            return SetValue(other.Get<T>());
        }

        public override bool SetDefault()
        {
            return SetValue(default(T));
        }

        private T m_Value;
    }

    class VFXShaderValueFloat : VFXShaderValue<float>           { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat; }}}
    class VFXShaderValueFloat2 : VFXShaderValue<Vector2>        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat2; }}}
    class VFXShaderValueFloat3 : VFXShaderValue<Vector3>        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat3; }}}
    class VFXShaderValueFloat4 : VFXShaderValue<Vector4>        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat4; }}}
    class VFXShaderValueInt : VFXShaderValue<int>               { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kInt; }}}
    class VFXShaderValueUint : VFXShaderValue<uint>             { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kUint; }}}
    class VFXShaderValueTexture2D : VFXShaderValue<Texture2D>   { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kTexture2D; }}}
    class VFXShaderValueTexture3D : VFXShaderValue<Texture3D>   { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kTexture3D; }}}
}