using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2DArray))]
    class VFXSlotTexture2DArray : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture2DArrayValue(0, mode);
        }
    }
}
