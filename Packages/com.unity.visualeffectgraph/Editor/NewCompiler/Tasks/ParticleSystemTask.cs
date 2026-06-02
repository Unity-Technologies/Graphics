using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class ParticleSystemTask : ITask
    {
        public static UniqueDataKey ParticleDataBindingKey { get; } = new UniqueDataKey(nameof(ParticleDataBindingKey));
        public static UniqueDataKey GraphValuesBindingKey { get; } = new UniqueDataKey(nameof(GraphValuesBindingKey));
        public static UniqueDataKey CpuEventListBindingKey { get; } = new UniqueDataKey(nameof(CpuEventListBindingKey));

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
                    writeUsage?.Add(DataPath.Root);
                    return BindingUsage.Write;
                case var _ when dataKey == GraphValuesBindingKey:
                    writeUsage?.Add(DataPath.Root);
                    return BindingUsage.Write;
                default:
                    readUsage?.Add(DataPath.Root);
                    return BindingUsage.Read;
            }
        }
    }
}
