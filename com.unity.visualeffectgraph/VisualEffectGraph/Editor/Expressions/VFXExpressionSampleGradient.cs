using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleGradient : VFXExpression
    {
        public VFXExpressionSampleGradient() : this(VFXValue<Gradient>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleGradient(VFXExpression gradient, VFXExpression time)
            : base(Flags.ValidOnCPU, new VFXExpression[2] { gradient, time })
        {}

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXSampleGradient; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } }

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return new VFXExpressionSampleGradient(reducedParents[0], reducedParents[1]);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[1];
            var gradientReduce = constParents[0];

            var gradient = gradientReduce.Get<Gradient>();
            var time = timeReduce.Get<float>();
            return new VFXValue<Vector4>(gradient.Evaluate(time), VFXValue.Mode.Constant);
        }
    }
}
