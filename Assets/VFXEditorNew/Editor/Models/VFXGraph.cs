using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXGraph : VFXModel
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return true; // Can hold any model
        }

        private ScriptableObject m_Owner;
    }
}