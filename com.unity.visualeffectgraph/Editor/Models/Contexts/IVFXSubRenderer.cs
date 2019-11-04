using System;

namespace UnityEditor.VFX
{
    interface IVFXSubRenderer
    {
        bool hasShadowCasting { get; }
        bool hasMotionVector { get; }
        // TODO Add other per output rendering settings here
        int sortPriority { get; set; }
    }
}
