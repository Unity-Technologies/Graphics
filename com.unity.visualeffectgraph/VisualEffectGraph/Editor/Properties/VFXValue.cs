using UnityEngine;
using System;

namespace UnityEngine.Experimental.VFX
{
    public enum VFXValueType
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

    public abstract class VFXExpression
    {
        public T Get<T>() { return ((VFXValue<T>)this).GetValue(); }
        public bool Set<T>(T value) { return ((VFXValue<T>)this).SetValue(value); }
    }

    public abstract class VFXValue : VFXExpression
    {
        public static VFXValue Create(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:       return new VFXValueFloat();
                case VFXValueType.kFloat2:      return new VFXValueFloat2();
                case VFXValueType.kFloat3:      return new VFXValueFloat3();
                case VFXValueType.kFloat4:      return new VFXValueFloat4();
                case VFXValueType.kInt:         return new VFXValueInt();
                case VFXValueType.kUint:        return new VFXValueUint();
                case VFXValueType.kTexture2D:   return new VFXValueTexture2D();
                case VFXValueType.kTexture3D:   return new VFXValueTexture3D();
                default:
                    throw new ArgumentException("Invalid parameter type");
            }
        }

        // Create from concrete type
        public static VFXValue Create<T>()
        {
            Type t = typeof(T);
            if (t == typeof(float))             return new VFXValueFloat();
            if (t == typeof(Vector2))           return new VFXValueFloat2();
            if (t == typeof(Vector3))           return new VFXValueFloat3();
            if (t == typeof(Vector4))           return new VFXValueFloat4();
            if (t == typeof(int))               return new VFXValueInt();
            if (t == typeof(uint))              return new VFXValueUint();
            if (t == typeof(Texture2D))         return new VFXValueTexture2D();
            if (t == typeof(Texture3D))         return new VFXValueTexture3D();

            throw new ArgumentException("Invalid parameter type");
        }

        public abstract VFXValue Clone();
        public abstract bool SetDefault();

        public abstract bool SetValue(VFXValue other);

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; }}
    }

    abstract class VFXValue<T> : VFXValue
    {
        public override VFXValue Clone()
        {
            return  (VFXValue<T>)MemberwiseClone();
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

        public override bool SetValue(VFXValue other)
        {
            return SetValue(other.Get<T>());
        }

        public override bool SetDefault()
        {
            return SetValue(default(T));
        }

        private T m_Value;
    }

    class VFXValueFloat : VFXValue<float>           { public override VFXValueType ValueType { get { return VFXValueType.kFloat; }}}
    class VFXValueFloat2 : VFXValue<Vector2>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat2; }}}
    class VFXValueFloat3 : VFXValue<Vector3>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat3; }}}
    class VFXValueFloat4 : VFXValue<Vector4>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat4; }}}
    class VFXValueInt : VFXValue<int>               { public override VFXValueType ValueType { get { return VFXValueType.kInt; }}}
    class VFXValueUint : VFXValue<uint>             { public override VFXValueType ValueType { get { return VFXValueType.kUint; }}}
    class VFXValueTexture2D : VFXValue<Texture2D>   { public override VFXValueType ValueType { get { return VFXValueType.kTexture2D; }}}
    class VFXValueTexture3D : VFXValue<Texture3D>   { public override VFXValueType ValueType { get { return VFXValueType.kTexture3D; }}}
}