using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXValue : VFXExpression
    {
        protected VFXValue()
        {
            m_Flags |= Flags.Value | Flags.ValidOnGPU | Flags.ValidOnCPU;
        }

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXValueOp; } }

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return this;
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        abstract public VFXValue CopyExpression(bool isConst);

        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        sealed public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public abstract void SetContent(object value);
    }

    sealed class VFXValue<T> : VFXValue
    {
        public VFXValue(T content = default(T), bool isConst = true)
        {
            m_Content = content;
            if (isConst)
            {
                m_Flags |= Flags.Constant;
            }
        }

        sealed public override VFXValue CopyExpression(bool isConst)
        {
            var copy = new VFXValue<T>();
            copy.m_Content = m_Content;
            copy.m_Flags = m_Flags;
            if (isConst)
            {
                copy.m_Flags |= Flags.Constant;
            }
            else
            {
                copy.m_Flags &= ~Flags.Constant;
            }
            return copy;
        }

        private static readonly VFXValue s_Default = new VFXValue<T>();
        public static VFXValue Default { get { return s_Default; } }


        public T Get()
        {
            return m_Content;
        }

        public override object GetContent()
        {
            return Get();
        }

        public override void SetContent(object value)
        {
            m_Content = default(T);
            if (value != null)
            {
                var fromType = value.GetType();
                var toType = typeof(T);

                if (fromType == toType || toType.IsAssignableFrom(fromType))
                {
                    m_Content = (T)Convert.ChangeType(value, toType);
                }
                else
                {
                    var implicitMethod = fromType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == toType);
                    if (implicitMethod != null)
                    {
                        m_Content = (T)implicitMethod.Invoke(null, new object[] { value });
                    }
                    else
                    {
                        Debug.LogErrorFormat("Cannot cast from {0} to {1}", fromType, toType);
                    }
                }
            }
        }

        protected T m_Content;

        private static VFXValueType ToValueType()
        {
            Type t = typeof(T);
            if (t == typeof(float)) return VFXValueType.kFloat;
            if (t == typeof(Vector2)) return VFXValueType.kFloat2;
            if (t == typeof(Vector3)) return VFXValueType.kFloat3;
            if (t == typeof(Vector4)) return VFXValueType.kFloat4;
            if (t == typeof(int)) return VFXValueType.kInt;
            if (t == typeof(uint)) return VFXValueType.kUint;
            if (t == typeof(Texture2D)) return VFXValueType.kTexture2D;
            if (t == typeof(Texture3D)) return VFXValueType.kTexture3D;
            if (t == typeof(Matrix4x4)) return VFXValueType.kTransform;
            if (t == typeof(AnimationCurve)) return VFXValueType.kCurve;
            if (t == typeof(Gradient)) return VFXValueType.kColorGradient;
            if (t == typeof(Mesh)) return VFXValueType.kMesh;
            if (t == typeof(System.Collections.Generic.List<Vector3>)) return VFXValueType.kSpline;
            throw new ArgumentException("Invalid type");
        }

        static private readonly VFXValueType s_ValueType = ToValueType();
        sealed public override VFXValueType ValueType
        {
            get
            {
                return s_ValueType;
            }
        }
    }
}
