using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2DArray))]
    class VFXSlotTexture2DArray : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Texture2DArray>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
