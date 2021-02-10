using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(CameraBuffer))]
    class VFXSlotCameraBuffer : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXCameraBufferValue(0, mode);
        }
    }
}
