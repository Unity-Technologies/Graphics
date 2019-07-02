using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractSpawner : VFXBlock
    {
        public override VFXContextType compatibleContexts { get { return VFXContextType.Spawner; } }
        public override VFXDataType compatibleData { get { return VFXDataType.SpawnEvent; } }
        public abstract VFXTaskType spawnerType { get; }
        public virtual Type customBehavior { get { return null; } }
    }
}
