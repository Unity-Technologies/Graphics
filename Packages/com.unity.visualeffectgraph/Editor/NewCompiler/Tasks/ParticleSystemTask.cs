using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class ParticleSystemTask : ITask
    {
        public static UniqueDataKey ParticleDataBindingKey { get; } = new UniqueDataKey(nameof(ParticleDataBindingKey));
        public static UniqueDataKey GraphValuesBindingKey { get; } = new UniqueDataKey(nameof(GraphValuesBindingKey));
        public static UniqueDataKey CpuEventListBindingKey { get; } = new UniqueDataKey(nameof(CpuEventListBindingKey));

        /// <summary>
        /// Keys for the concrete buffers bound onto the system task once the data has been lowered. The
        /// names are the mapping names the runtime looks the buffers up by, so they are not free to change.
        /// </summary>
        public static class BufferKeys
        {
            public static NameDataKey AttributeBufferKey { get; } = new("attributeBuffer");
            public static NameDataKey SourceAttributeBufferKey { get; } = new("sourceAttributeBuffer");
            public static NameDataKey DeadListBufferKey { get; } = new("deadList");
            public static NameDataKey SpawnBufferKey { get; } = new("spawnBuffer");
            public static NameDataKey GraphValuesBufferKey { get; } = new("graphValuesBuffer");
            public static NameDataKey EventListBufferKey { get; } = new("eventList");
            public static NameDataKey SpawnerInputBufferKey { get; } = new("spawner_input");

            public static bool IsGpuBuffer(IDataKey dataKey) =>
                dataKey is NameDataKey key &&
                (key == AttributeBufferKey || key == SourceAttributeBufferKey || key == DeadListBufferKey ||
                 key == SpawnBufferKey || key == GraphValuesBufferKey || key == EventListBufferKey);

            public static bool IsCpuBuffer(IDataKey dataKey) =>
                dataKey is NameDataKey key && key == SpawnerInputBufferKey;
        }

        public System.Type GetBindingExpectedDataType(IDataKey dataKey)
        {
            switch (dataKey)
            {
                case SpawnerDataKey:
                    return typeof(EventData);
                case var _ when dataKey == CpuEventListBindingKey:
                    return typeof(EventListData);
                case var _ when dataKey == ParticleDataBindingKey:
                    return typeof(ParticleData);
                case var _ when dataKey == GraphValuesBindingKey:
                    return typeof(StructuredData);
                case var _ when BufferKeys.IsGpuBuffer(dataKey):
                    return typeof(VfxGpuBufferData);
                case var _ when BufferKeys.IsCpuBuffer(dataKey):
                    return typeof(VfxCpuBufferData);
                default:
                    return typeof(ValueData);
            }
        }

        /// <inheritdoc />
        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            switch (dataKey)
            {
                case SpawnerDataKey:
                    readUsage?.Add(DataPath.Root + EventData.AttributeDataKey);
                    return BindingUsage.Read;
                case var _ when dataKey == CpuEventListBindingKey:
                    writeUsage?.Add(DataPath.Root + EventData.AttributeDataKey);
                    return BindingUsage.Write;
                case var _ when dataKey == ParticleDataBindingKey:
                    writeUsage?.Add(DataPath.Root + ParticleData.AttributeDataKey
                        + new AttributeKey(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.Alive)));
                    writeUsage?.Add(DataPath.Root + ParticleData.DeadlistKey);
                    return BindingUsage.Write;
                case var _ when dataKey == GraphValuesBindingKey:
                    writeUsage?.Add(DataPath.Root);
                    return BindingUsage.Write;
                case var _ when BufferKeys.IsGpuBuffer(dataKey):
                    writeUsage?.Add(DataPath.Root);
                    return BindingUsage.Write;
                case var _ when BufferKeys.IsCpuBuffer(dataKey):
                    readUsage?.Add(DataPath.Root);
                    return BindingUsage.Read;
                default:
                    readUsage?.Add(DataPath.Root);
                    return BindingUsage.Read;
            }
        }
    }
}
