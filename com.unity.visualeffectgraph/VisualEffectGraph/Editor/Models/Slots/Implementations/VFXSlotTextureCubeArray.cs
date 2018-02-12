using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(CubemapArray))]
    class VFXSlotTextureCubeArray : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<CubemapArray>(null, mode);
        }
    }
}
