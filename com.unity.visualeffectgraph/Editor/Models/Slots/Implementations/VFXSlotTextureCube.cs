using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTextureCubeValue(0, mode);
        }
    }
}
