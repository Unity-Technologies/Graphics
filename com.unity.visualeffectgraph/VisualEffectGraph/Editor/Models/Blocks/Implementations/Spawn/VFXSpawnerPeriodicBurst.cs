using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Obsolete]
    class VFXSpawnerPeriodicBurst : VFXAbstractSpawner
    {
        public override string name { get { return "PeriodicBurst (DEPRECATED)"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.PeriodicBurstSpawner; } }
        public class InputProperties
        {
            public Vector2 nb = new Vector2(0, 10);
            public Vector2 period = new Vector2(0, 1);
        }

        public override void Sanitize()
        {
            var newBlock = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            newBlock.Repeat = VFXSpawnerBurst.RepeatMode.Periodic;
            newBlock.DelayMode = VFXSpawnerBurst.RandomMode.Random;
            newBlock.SpawnMode = VFXSpawnerBurst.RandomMode.Random;

            VFXSlot.TransferLinksAndValue(newBlock.GetInputSlot(0), GetInputSlot(0), true);
            VFXSlot.TransferLinksAndValue(newBlock.GetInputSlot(1), GetInputSlot(1), true);

            ReplaceModel(newBlock, this);
            base.Sanitize();
        }
    }
}
