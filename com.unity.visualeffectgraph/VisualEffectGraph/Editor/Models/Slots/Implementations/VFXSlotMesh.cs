using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Mesh))]
    class VFXSlotMesh : VFXSlot
    {
        public override VFXValue DefaultExpression()
        {
            return new VFXValue<Mesh>(null, VFXValue.Mode.FoldableVariable);
        }
    }
}
