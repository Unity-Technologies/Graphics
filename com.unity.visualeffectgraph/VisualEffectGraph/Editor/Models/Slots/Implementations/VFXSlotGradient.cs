using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Gradient))]
    class VFXSlotGradient : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<Gradient>(new Gradient(), VFXValue.Mode.FoldableVariable);
        }
    }
}
