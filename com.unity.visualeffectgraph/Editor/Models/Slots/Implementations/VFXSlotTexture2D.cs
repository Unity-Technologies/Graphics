using System;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture2DValue(0, mode);
        }
    }
}
