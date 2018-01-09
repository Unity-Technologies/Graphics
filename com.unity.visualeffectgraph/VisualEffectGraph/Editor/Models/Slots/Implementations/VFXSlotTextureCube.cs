using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Cubemap>(null, mode);
        }
    }
}
