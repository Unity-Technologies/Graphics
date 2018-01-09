using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2DArray))]
    class VFXSlotTexture2DArray : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Texture2DArray>(null, mode);
        }
    }
}
