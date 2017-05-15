using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot 
    {
        // TODO Add VFXValueType.Boolean
        /*protected override VFXValue DefaultExpression()
        {
            return new VFXValue<bool>(false, false);
        }*/
    }
}
