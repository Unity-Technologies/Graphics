using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXValue : VFXExpression
    {
        public enum Mode
        {
            Variable, // Variable that should never be folded
            FoldableVariable, // Variable that can be folded
            Constant, // Immutable value
        }

        protected VFXValue(Mode mode)
            : base(Flags.Value | Flags.ValidOnGPU | Flags.ValidOnCPU)
        {
            m_Mode = mode;
            if (mode != Mode.Variable)
                m_Flags |= Flags.Constant;
        }

        public Mode ValueMode { get { return m_Mode; } }

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXValueOp; } }

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return this;
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        abstract public VFXValue CopyExpression(Mode mode);

        public override bool Equals(object obj)
        {
            if (m_Mode == Mode.Constant)
            {
                var val = obj as VFXValue;
                if (val == null)
                    return false;

                if (ValueType != val.ValueType)
                    return false;

                var content = GetContent();
                var otherContent = val.GetContent();

                if (content == null)
                    return otherContent == null;

                return content.Equals(otherContent);
            }

            return ReferenceEquals(this, obj);
        }

        sealed public override int GetHashCode()
        {
            if (m_Mode == Mode.Constant)
            {
                int hashCode = ValueType.GetHashCode();
                var content = GetContent();

                if (content != null)
                    hashCode ^= content.GetHashCode();

                return hashCode;
            }

            return RuntimeHelpers.GetHashCode(this);
        }

        public abstract void SetContent(object value);

        private Mode m_Mode;
    }

    sealed class VFXValue<T> : VFXValue
    {
        public VFXValue(T content = default(T), Mode mode = Mode.FoldableVariable) : base(mode)
        {
            m_Content = content;
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXValue<T>(m_Content, mode);
            return copy;
        }

        private static readonly VFXValue s_Default = new VFXValue<T>(default(T), VFXValue.Mode.Constant);
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
            if (ValueMode == Mode.Constant)
                throw new InvalidOperationException("Cannot set content of an immutable value");

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
