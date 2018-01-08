using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(AnimationCurve))]
    class VFXSlotAnimationCurve : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<AnimationCurve>(new AnimationCurve(), VFXValue.Mode.FoldableVariable);
        }
    }
}
