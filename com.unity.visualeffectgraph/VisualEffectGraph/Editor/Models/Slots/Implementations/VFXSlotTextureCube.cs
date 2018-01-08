using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<Cubemap>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
