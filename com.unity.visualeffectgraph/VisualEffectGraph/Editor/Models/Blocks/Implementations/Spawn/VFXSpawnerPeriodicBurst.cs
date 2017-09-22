using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerPeriodicBurst : VFXAbstractSpawner
    {
        public override string name { get { return "PeriodicBurst"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.kSpawnerPeriodicBurst; } }
        public class InputProperties
        {
            public float nb = 10;
            public float period = 1;
        }
    }
}
