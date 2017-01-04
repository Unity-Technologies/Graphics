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

        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXValueOp; } }
        public override VFXExpression Reduce(VFXExpressionContext context) { return this; }

        public override bool Equals(object obj) { return ReferenceEquals(this,obj); }
        public override int GetHashCode()       { return RuntimeHelpers.GetHashCode(this); }
    }

    abstract class VFXValue<T> : VFXValue
    {
        public override string ToString() { return m_Value.ToString(); }

        protected T m_Value;
    }
}