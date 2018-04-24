using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Obsolete]
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerPeriodicBurst : VFXAbstractSpawner
    {
        public override string name { get { return "PeriodicBurst (DEPRECATED)"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.PeriodicBurstSpawner; } }
        public class InputProperties
        {
            public Vector2 nb = new Vector2(0, 10);
            public Vector2 period = new Vector2(0, 1);
        }
    }
}
