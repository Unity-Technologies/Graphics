using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Represents a task generated using a template and a list of snippets.
    /// </summary>
    /*public*/ class SpawnerTask : ITask
    {
        /// <summary>
        /// Gets the name of the template associated with the task.
        /// </summary>
        public VFXTaskType SpawnerType { get; }

        public IDataKey SpawnDataKey { get; }

        public Attribute Attribute { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpawnerTask"/>
        /// </summary>
        /// <param name="spawnerType">The spawner task type.</param>
        /// <param name="spawnDataKey"> The data key for the spawn data</param>
        public SpawnerTask(VFXTaskType spawnerType, IDataKey spawnDataKey, Attribute attribute = null)
        {
            SpawnerType = spawnerType;
            SpawnDataKey = spawnDataKey;
            Attribute = attribute;
        }

        /// <inheritdoc />
        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            BindingUsage usage = BindingUsage.Unknown;

            if (dataKey.Equals(SpawnDataKey))
            {
                if (writeUsage != null)
                {
                    writeUsage.Add(DataPath.Root);
                    DataPath attributeDataPath = DataPath.Root + EventData.AttributeDataKey;
                    writeUsage.Add(attributeDataPath);
                    if (Attribute != null)
                    {
                        writeUsage.Add(attributeDataPath + new AttributeKey(Attribute));
                    }
                }
                usage |= BindingUsage.Write;
            }
            else if (dataKey is NameDataKey) // Expression values are read by the spawner
            {
                readUsage?.Add(DataPath.Root);
                usage |= BindingUsage.Read;
            }

            return usage;
        }
    }
}
