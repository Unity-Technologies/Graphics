using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXDataSpawnEvent : VFXData
    {
        public override VFXDataType type { get { return VFXDataType.kSpawnEvent; } }
    }
}
