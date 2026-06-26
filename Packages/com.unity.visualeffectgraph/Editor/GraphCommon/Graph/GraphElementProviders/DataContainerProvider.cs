namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        class DataContainerProvider : IDataContainerProvider
        {
            TaskGraph m_Owner;

            public int Count => m_Owner.m_DataContainers.Count;

            public DataContainer this[DataContainerId dataContainerId] => new(m_Owner, m_Owner.m_DataContainers[dataContainerId]);

            public DataContainerProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public DataContainerEnumerator GetEnumerator() => new(m_Owner.m_DataContainers, this);
        }
    }
}
