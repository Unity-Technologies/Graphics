namespace Unity.GraphCommon.LowLevel.Editor
{
    class DataContainerProvider : IDataContainerProvider
    {
        readonly TaskGraph m_Owner;
        readonly ReadOnlyTaskGraphStore m_Store;

        public int Count => m_Store.DataContainers.Count;

        public DataContainer this[DataContainerId dataContainerId] => new(m_Owner, m_Store.DataContainers[dataContainerId]);

        public DataContainerProvider(TaskGraph owner, ReadOnlyTaskGraphStore store)
        {
            m_Owner = owner;
            m_Store = store;
        }

        public DataContainerEnumerator GetEnumerator() => new(m_Store.DataContainers, this);
    }
}
