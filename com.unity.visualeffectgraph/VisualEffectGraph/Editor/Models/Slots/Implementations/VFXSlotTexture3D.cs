using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Texture3D>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
