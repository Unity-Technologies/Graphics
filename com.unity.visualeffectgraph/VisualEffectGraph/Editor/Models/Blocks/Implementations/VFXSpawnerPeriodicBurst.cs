using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerPeriodicBurst : VFXAbstractSpawner
    {
        public override string name { get { return "PeriodicBurst"; } }
        public override VFXSpawnerType spawnerType { get { return VFXSpawnerType.kPeriodicBurst; } }
        public class InputProperties
        {
            public float nb = 10;
            public float period = 1;
        }
    }
}
