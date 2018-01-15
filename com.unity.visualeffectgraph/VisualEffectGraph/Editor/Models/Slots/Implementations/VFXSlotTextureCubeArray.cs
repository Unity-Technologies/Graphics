using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(CubemapArray))]
    class VFXSlotTextureCubeArray : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<CubemapArray>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
