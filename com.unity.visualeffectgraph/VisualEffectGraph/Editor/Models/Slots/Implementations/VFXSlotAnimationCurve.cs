using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(AnimationCurve))]
    class VFXSlotAnimationCurve : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<AnimationCurve>(new AnimationCurve(), true);
        }
    }
}
