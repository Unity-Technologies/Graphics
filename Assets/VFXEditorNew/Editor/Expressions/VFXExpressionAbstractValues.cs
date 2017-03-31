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

        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        sealed public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }
    }

    abstract class VFXValue<T> : VFXValue
    {
        protected VFXValue(T content = default(T), bool isConst = true)
        {
            m_Content = content;
            if (isConst)
            {
                m_Flags |= Flags.Constant;
            }
        }

        private static VFXValue FindAndCreateFirstConcreteType()
        {
            var firstConcreteType = typeof(VFXValue<T>)
                                    .Assembly
                                    .GetTypes()
                                    .Where(t => t.IsSubclassOf(typeof(VFXValue<T>)) && !t.IsAbstract)
                                    .First();
            return CreateNewInstance(firstConcreteType) as VFXValue;
        }
        private static readonly VFXValue s_Default = FindAndCreateFirstConcreteType();
        public static VFXValue Default { get { return s_Default; } }

        public T GetContent()
        {
            return m_Content;
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