using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Texture2D>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
