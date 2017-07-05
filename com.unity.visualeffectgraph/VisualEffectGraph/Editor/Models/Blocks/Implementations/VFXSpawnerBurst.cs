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
            public Vector2 Count = new Vector2(10, 10);
            public Vector2 Delay = new Vector2(1, 1);
        }
    }
}
