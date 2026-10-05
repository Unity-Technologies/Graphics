namespace Unity.GraphCommon.LowLevel.Editor
{
    class DataBindingProvider : IDataBindingProvider
    {
        readonly TaskGraph m_Owner;
        readonly ReadOnlyTaskGraphStore m_Store;

        public int Count => m_Store.DataBindings.Count;

        public DataBinding this[DataBindingId dataBindingId] => new(m_Owner, m_Store.DataBindings[dataBindingId]);

        public DataBindingProvider(TaskGraph owner, ReadOnlyTaskGraphStore store)
        {
            m_Owner = owner;
            m_Store = store;
        }

        public DataBindingEnumerator GetEnumerator() => new(m_Store.DataBindings, this);
    }
}
