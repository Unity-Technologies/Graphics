using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionBakeGradient : VFXExpression
    {
        public VFXExpressionBakeGradient() : this(VFXValue<Gradient>.Default)
        {
        }

        public VFXExpressionBakeGradient(VFXExpression curve)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { curve })
        {
        }

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXBakeGradient; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat; } }
    }
}
