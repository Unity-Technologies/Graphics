using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerVariableRate : VFXAbstractSpawner
    {
        public override string name { get { return "VariableRate"; } }
        public override VFXSpawnerType spawnerType { get { return VFXSpawnerType.kVariableRate; } }
        public class InputProperties
        {
            public float nb = 10;
            public float period = 1;
        }
    }
}
