using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerConstantRate : VFXAbstractSpawner
    {
        public override string name { get { return "ConstantRate"; } }
        public override VFXSpawnerType spawnerType { get { return VFXSpawnerType.kConstantRate; } }
        public class InputProperties
        {
            public float Rate = 10;
        }
    }
}
