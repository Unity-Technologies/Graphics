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

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return new VFXExpressionBakeGradient(reducedParents[0]);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            throw new NotImplementedException(); //Cannot constant fold kVFXBakeGradient in C#
        }

        public sealed override string GetCodeString(string[] parents)
        {
            throw new NotImplementedException();
        }
    }
}
