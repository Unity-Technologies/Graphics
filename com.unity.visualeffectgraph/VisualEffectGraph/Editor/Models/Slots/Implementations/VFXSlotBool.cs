using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<bool>(false, VFXValue.Mode.FoldableVariable);
        }
    }
}
