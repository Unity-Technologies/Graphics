using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Matrix4x4))]
    class VFXSlotMatrix4x4 : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, VFXValue.Mode.FoldableVariable);
        }
    }
}
