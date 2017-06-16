using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractSpawner : VFXBlock
    {
        public override VFXContextType compatibleContexts { get { return VFXContextType.kSpawner; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kSpawnEvent; } }

        public abstract VFXSpawnerType spawnerType { get; }
    }
}
