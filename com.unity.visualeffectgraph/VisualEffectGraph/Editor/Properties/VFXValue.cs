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

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; } }

        public virtual bool IsValue() { return false; }
        public virtual bool IsConst() { return false; } // Allow constant propagation (TODO)
         
        // Reduce the expression and potentially cache the result before returning it
        public abstract VFXExpression Reduce();
        // Invalidate the reduction to impose a recomputation
        public abstract void Invalidate();
    }

    public abstract class VFXValue : VFXExpression
    {
        public static string TypeToName(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:   return "float";
                case VFXValueType.kFloat2:  return "float2";
                case VFXValueType.kFloat3:  return "float3";
                case VFXValueType.kFloat4:  return "float4";
                case VFXValueType.kInt:     return "int";
                case VFXValueType.kUint:    return "uint";
                default:                
                    return "";
            }
        }

        // Return type size (in dword)
        public static int TypeToSize(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:   return 1;
                case VFXValueType.kFloat2:  return 2;
                case VFXValueType.kFloat3:  return 3;
                case VFXValueType.kFloat4:  return 4;
                case VFXValueType.kInt:     return 1;
                case VFXValueType.kUint:    return 1;
                default:
                    return 0;
            }
        }

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
                    return null;
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

        public static VFXValue Create<T>(T value)
        {
            VFXValue v = Create<T>();
            v.Set(value);
            return v;
        }

        public override bool IsValue() { return true; }

        public abstract VFXValue Clone();
        public abstract bool SetDefault();

        public abstract bool SetValue(VFXValue other);

        public override VFXExpression Reduce()  { return this; }    // Already reduced
        public override void Invalidate()       {}                  // No cache to invalidate
    }

    abstract class VFXValue<T> : VFXValue
    {
        public override VFXValue Clone()
        {
            var clone = VFXValue.Create<T>();
            clone.SetValue(this);
            return clone;
        }

        public VFXValue()
        {
            ConstrainValue(); // In order to constraint default value
        }

        public T GetValue() { return m_Value; }
        public bool SetValue(T t)
        {
            if (IsDifferent(m_Value,t))
            {
                T oldValue = m_Value;
                m_Value = t;
                ConstrainValue();
                return IsDifferent(m_Value, oldValue); // Recheck as the constrain may have changed the value
            }
            return false;
        }

        private static bool IsDifferent(T t0,T t1)
        {
            return (t0 == null && t1 != null) || (t0 != null && !t0.Equals(t1));
        }

        public override bool SetValue(VFXValue other)
        {
            return SetValue(other.Get<T>());
        }

        public override bool SetDefault()
        {
            return SetValue(default(T));
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }

        protected virtual void ConstrainValue() {}

        protected T m_Value;
    }

    class VFXValueFloat : VFXValue<float>           { public override VFXValueType ValueType { get { return VFXValueType.kFloat; }}}
    class VFXValueFloat2 : VFXValue<Vector2>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat2; }}}
    class VFXValueFloat3 : VFXValue<Vector3>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat3; }}}
    class VFXValueFloat4 : VFXValue<Vector4>        { public override VFXValueType ValueType { get { return VFXValueType.kFloat4; }}}
    class VFXValueInt : VFXValue<int>               { public override VFXValueType ValueType { get { return VFXValueType.kInt; }}}
    class VFXValueUint : VFXValue<uint>             { public override VFXValueType ValueType { get { return VFXValueType.kUint; }}}
    
    class VFXValueTexture2D : VFXValue<Texture2D>   
    { 
        public override VFXValueType ValueType { get { return VFXValueType.kTexture2D; }}

        protected override void ConstrainValue() 
        {
            // Replace null texture value by white texture placeholder
            if (m_Value == null)
                m_Value = Texture2D.whiteTexture;
        }
    }

    class VFXValueTexture3D : VFXValue<Texture3D>   { public override VFXValueType ValueType { get { return VFXValueType.kTexture3D; }}}

    class VFXExpressionCombineFloat3 : VFXExpression
    {
        public VFXExpressionCombineFloat3(VFXExpression x, VFXExpression y, VFXExpression z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_Cached = new VFXValueFloat3();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_X.IsValue() && m_Y.IsValue() && m_Z.IsValue())
            {
                m_Cached.Set(new Vector3(m_X.Get<float>(),m_Y.Get<float>(),m_Z.Get<float>()));
                m_CacheValid = true;
                return m_Cached;
            }

            return this; 
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        private VFXExpression m_X;
        private VFXExpression m_Y;
        private VFXExpression m_Z;

        private VFXValueFloat3 m_Cached;
        private bool m_CacheValid;
    }
}