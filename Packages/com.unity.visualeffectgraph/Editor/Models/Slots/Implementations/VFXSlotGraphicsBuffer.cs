using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(GraphicsBuffer))]
    class VFXSlotGraphicsBuffer : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXGraphicsBufferValue(0, mode);
        }
    }
}
