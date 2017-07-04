using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerBurst : VFXAbstractSpawner
    {
        public override string name { get { return "Burst"; } }
        public override VFXSpawnerType spawnerType { get { return VFXSpawnerType.kBurst; } }
        public class InputProperties
        {
            public float Count = 10;
            public float Delay = 1;
        }
    }
}
