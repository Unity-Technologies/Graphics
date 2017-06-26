using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleCurve : VFXExpression
    {
        public VFXExpressionSampleCurve() : this(VFXValue<AnimationCurve>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleCurve(VFXExpression curve, VFXExpression time)
            : base(Flags.None, new VFXExpression[2] { curve, time })
        {}

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXSampleCurve; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat; } }

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return new VFXExpressionSampleCurve(reducedParents[0], reducedParents[1]);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[1];
            var curveReduce = constParents[0];

            var curve = curveReduce.Get<AnimationCurve>();
            var time = timeReduce.Get<float>();
            return new VFXValue<float>(curve.Evaluate(time), VFXValue.Mode.Constant);
        }
    }
}
