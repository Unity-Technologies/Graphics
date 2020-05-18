using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(SkinnedMeshRenderer))]
    class VFXSlotSkinnedMeshRenderer : VFXSlotObject
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXSkinnedMeshRendererValue(0, mode);
        }
    }
}
