using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    interface IVFXSubRenderer
    {
        bool hasShadowCasting { get; }
        bool hasMotionVector { get; }
        // TODO Add other per output rendering settings here
        int sortPriority { get; set; }

        // Allow to setup material generated during import
        void SetupMaterial(Material material);
    }
}
