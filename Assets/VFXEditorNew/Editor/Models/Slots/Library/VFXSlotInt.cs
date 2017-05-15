using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(int))]
    class VFXSlotInt : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<int>(0, false);
        }
    }
}
