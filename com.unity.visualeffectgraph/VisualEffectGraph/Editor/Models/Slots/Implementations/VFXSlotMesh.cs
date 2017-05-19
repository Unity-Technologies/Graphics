using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Mesh))]
    class VFXSlotMesh : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Mesh>(null, true);
        }
    }
}
