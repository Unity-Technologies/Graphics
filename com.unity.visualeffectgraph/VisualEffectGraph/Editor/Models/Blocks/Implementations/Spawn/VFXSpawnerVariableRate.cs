using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerVariableRate : VFXAbstractSpawner
    {
        public override string name { get { return "VariableRate"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.VariableRateSpawner; } }
        public class InputProperties
        {
            public Vector2 nb = new Vector2(0, 10);
            public Vector2 period = new Vector2(0, 1);
        }
    }
}
