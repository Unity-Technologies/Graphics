using System;
using System.Collections.Generic;
using UnityEngine;

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
        kTransform,
        kCurve,
        kColorGradient,
    }

    public abstract class VFXExpression
    {
        public T Get<T>() { return ((VFXValue<T>)(this.Reduce())).GetValue(); } // Always try to reduce the value before getting it
        public bool Set<T>(T value) { return ((VFXValue<T>)this).SetValue(value); }

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; } }
        public abstract VFXExpressionOp Operation { get; }

        public virtual bool IsValue(bool reduced = true)    { return reduced ? Reduce().IsValue(false) : false; }
        public virtual bool IsConst()                       { return false; } // Allow constant propagation (TODO)
         
        // Reduce the expression and potentially cache the result before returning it
        public abstract VFXExpression Reduce();
        // Invalidate the reduction to impose a recomputation
        public abstract void Invalidate();
        // Returns dependencies
        public virtual VFXExpression[] GetParents() { return null; }
    }

    public abstract class VFXValue : VFXExpression
    {
        public static string TypeToName(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:       return "float";
                case VFXValueType.kFloat2:      return "float2";
                case VFXValueType.kFloat3:      return "float3";
                case VFXValueType.kFloat4:      return "float4";
                case VFXValueType.kInt:         return "int";
                case VFXValueType.kUint:        return "uint";
                case VFXValueType.kTransform:   return "float4x4"; // tmp we want to optimize that
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

        public static VFXValueType ToValueType<T>()
        {
            Type t = typeof(T);
            if (t == typeof(float))             return VFXValueType.kFloat;
            if (t == typeof(Vector2))           return VFXValueType.kFloat2;
            if (t == typeof(Vector3))           return VFXValueType.kFloat3;
            if (t == typeof(Vector4))           return VFXValueType.kFloat4;
            if (t == typeof(int))               return VFXValueType.kInt;
            if (t == typeof(uint))              return VFXValueType.kUint;
            if (t == typeof(Texture2D))         return VFXValueType.kTexture2D;
            if (t == typeof(Texture3D))         return VFXValueType.kTexture3D;
            if (t == typeof(Matrix4x4))         return VFXValueType.kTransform;
            if (t == typeof(AnimationCurve))    return VFXValueType.kCurve;
            if (t == typeof(Gradient))          return VFXValueType.kColorGradient;

            throw new ArgumentException("Invalid type");
        }

        public static VFXValue Create(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kFloat:           return new VFXValueFloat();
                case VFXValueType.kFloat2:          return new VFXValueFloat2();
                case VFXValueType.kFloat3:          return new VFXValueFloat3();
                case VFXValueType.kFloat4:          return new VFXValueFloat4();
                case VFXValueType.kInt:             return new VFXValueInt();
                case VFXValueType.kUint:            return new VFXValueUint();
                case VFXValueType.kTexture2D:       return new VFXValueTexture2D();
                case VFXValueType.kTexture3D:       return new VFXValueTexture3D();
                case VFXValueType.kTransform:       return new VFXValueTransform();
                case VFXValueType.kCurve:           return new VFXValueCurve();
                case VFXValueType.kColorGradient:   return new VFXValueColorGradient();
                default:
                    return null;
            }
        }

        // Create from concrete type
        public static VFXValue Create<T>()
        {
            return Create(ToValueType<T>());
        }

        public static VFXValue Create<T>(T value)
        {
            VFXValue v = Create<T>();
            v.Set(value);
            return v;
        }

        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXValueOp; } }

        public override bool IsValue(bool reduced) { return true; }

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

    class VFXValueTransform : VFXValue<Matrix4x4>   
    { 
        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }

        public override bool SetDefault()
        {
            return SetValue(new Matrix4x4());
        }
    }

    class VFXValueCurve : VFXValue<AnimationCurve>      { public override VFXValueType ValueType { get { return VFXValueType.kCurve; }}}
    class VFXValueColorGradient : VFXValue<Gradient>    { public override VFXValueType ValueType { get { return VFXValueType.kColorGradient; }}}
}