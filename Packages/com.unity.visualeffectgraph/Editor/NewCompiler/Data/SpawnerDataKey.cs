using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    record SpawnerDataKey : IDataKey
    {
        public VFXBasicSpawner Spawner { get; }

        public SpawnerDataKey(VFXBasicSpawner spawner)
        {
            Spawner = spawner;
        }

        public override string ToString() => Spawner.name;
    }
}
