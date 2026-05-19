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
        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            if (dataKey.Equals(SpawnDataKey))
            {
                readUsage = new DataPathSet();
                writeUsage = new DataPathSet();
                writeUsage.Add(DataPath.Empty);
                DataPath attributeDataPath = new(EventData.AttributeDataKey);
                writeUsage.Add(attributeDataPath);
                if (Attribute != null)
                {
                    writeUsage.Add(new DataPath(attributeDataPath, new AttributeKey(Attribute)));
                }
                return true;
            }
            readUsage = null;
            writeUsage = null;
            return false;
        }
    }
}
