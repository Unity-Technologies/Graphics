using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Matrix4x4))]
    class VFXSlotMatrix4x4 : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, mode);
        }
    }
}
