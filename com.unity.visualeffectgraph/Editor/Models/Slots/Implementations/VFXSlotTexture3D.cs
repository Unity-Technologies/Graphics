using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;


namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture3DValue(0, mode);
        }
    }
}
