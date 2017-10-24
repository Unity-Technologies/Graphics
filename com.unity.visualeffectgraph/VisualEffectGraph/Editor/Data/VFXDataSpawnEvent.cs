using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXDataSpawnEvent : VFXData
    {
        public override VFXDataType type { get { return VFXDataType.kSpawnEvent; } }

        public override bool CanBeCompiled()
        {
            return m_Owners.Count != 0 && m_Owners[0].outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
