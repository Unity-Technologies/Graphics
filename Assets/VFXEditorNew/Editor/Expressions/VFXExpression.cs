using System;
using UnityEngine;

namespace UnityEditor.VFX
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
        kSpline,
    }

    abstract class VFXExpression
    {
        [Flags]
        public enum Flags
        {
            None =          0,
            Value =         1 << 0, // Expression is a value, get/set can be called on it
            Constant =      1 << 1, // Expression is a constant, it can be folded
            ValidOnGPU =    1 << 2, // Expression can be evaluated on GPU
            ValidOnCPU =    1 << 3, // Expression can be evaluated on CPU
        }

        public enum ReductionOption
        {
            CPUEvaluation = 0,
            ConstantFolding = 1,
        }

        public bool Is(Flags flag) { return (m_Flags & flag) == flag; }

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; } }
        public abstract VFXExpressionOp Operation { get; }

        // Reduce the expression within a given context
        public abstract VFXExpression Reduce(VFXExpressionContext context);
        // Get a string representation of the expression
        public abstract string Stringify(VFXExpressionContext context);
        // Returns dependencies
        public virtual VFXExpression[] GetParents() { return null; }

        public override bool Equals(object obj)
        {
            var other = obj as VFXExpression;
            if (other == null)
                return false;

            if (Operation != other.Operation)
                return false;

            if (GetHashCode() != obj.GetHashCode())
                return false;

            // TODO Not really optimized for an equal function!
            var thisParents = GetParents();
            var otherParents = other.GetParents();

            if (thisParents == null && otherParents == null)
                return true;
            if (thisParents == null || otherParents == null)
                return false;
            if (thisParents.Length != otherParents.Length)
                return false;

            for (int i = 0; i < thisParents.Length; ++i)
                if (!thisParents[i].Equals(otherParents[i]))
                    return false;

            return true;         
        }

        public override int GetHashCode()
        {
            if (!m_HasCachedHashCode)
            {
                int hash = GetType().GetHashCode();

                var parents = GetParents();
                if (parents != null)
                    for (int i = 0; i < parents.Length; ++i)
                        hash = (hash * 397) ^ parents[i].GetHashCode(); // 397 taken from resharper

                m_CachedHashCode = hash;
                m_HasCachedHashCode = true;
            }

            return m_CachedHashCode;
        }

        protected Flags m_Flags = Flags.None;

        protected int m_CachedHashCode;
        protected bool m_HasCachedHashCode = false;
    }
}
