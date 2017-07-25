using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(uint))]
    class VFXSlotUint : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<uint>(0u, VFXValue.Mode.FoldableVariable);
        }
    }
}
