using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(float))]
    class VFXSlotFloat : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValueFloat(0.0f, false);
        }
    }
}
