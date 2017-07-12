using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionBakeCurve : VFXExpression
    {
        public VFXExpressionBakeCurve() : this(VFXValue<AnimationCurve>.Default)
        {
        }

        public VFXExpressionBakeCurve(VFXExpression curve)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { curve })
        {
        }

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXBakeCurve; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } }
    }
}
