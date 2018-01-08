using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<bool>(false, VFXValue.Mode.FoldableVariable);
        }
    }
}
