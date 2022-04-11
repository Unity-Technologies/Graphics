using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerConstantRate : VFXAbstractSpawner
    {
        public override string name { get { return "Constant Spawn Rate"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.ConstantRateSpawner; } }
        public class InputProperties
        {
            [Min(0), Tooltip("Sets the number of particles to be spawned per second.")]
            public float Rate = 10;
        }
    }
}
