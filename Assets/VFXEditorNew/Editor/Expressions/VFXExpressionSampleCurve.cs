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
        {
            m_Flags = Flags.ValidOnCPU;
            m_Curve = curve;
            m_Time = time;
        }

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXSampleCurve; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat; } }

        sealed public override VFXExpression[] Parents
        {
            get
            {
                return new VFXExpression[] { m_Curve, m_Time };
            }
        }

        sealed protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            return new VFXExpressionSampleCurve(reducedParents[0], reducedParents[1]);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[0];
            var curveReduce = constParents[1];

            var curve = curveReduce.Get<AnimationCurve>();
            var time = timeReduce.Get<float>();
            return new VFXValue<float>(curve.Evaluate(time), true);
        }

        private VFXExpression m_Curve;
        private VFXExpression m_Time;
    }
}
