namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        class DataBindingProvider : IDataBindingProvider
        {
            TaskGraph m_Owner;

            public int Count => m_Owner.m_DataBindings.Count;

            public DataBinding this[DataBindingId dataBindingId] => new(m_Owner, m_Owner.m_DataBindings[dataBindingId]);

            public DataBindingProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public DataBindingEnumerator GetEnumerator() => new(m_Owner.m_DataBindings, this);
        }
    }
}
