namespace Unity.GraphCommon.LowLevel.Editor
{
    // Read-only window onto a GraphDataList the caller does not own. Reads pass through, so it sees
    // every later mutation by the owner; it just cannot make one.
    readonly struct ReadOnlyGraphDataList<T> where T : struct
    {
        readonly GraphDataList<T> m_List;

        public ReadOnlyGraphDataList(GraphDataList<T> list) => m_List = list;

        public static implicit operator ReadOnlyGraphDataList<T>(GraphDataList<T> list) => new(list);

        public int Count => m_List.Count;
        public int SlotCount => m_List.SlotCount;

        // By value, not by ref: a ref return would be capped by the scope of this struct when it is a
        // temporary (store.DataViews.AtSlot(i)), and no caller needs one — they read a field or copy.
        public T this[GraphDataId id] => m_List[id];

        public bool IsAlive(GraphDataId id) => m_List.IsAlive(id);
        public bool IsAliveAtSlot(int slot) => m_List.IsAliveAtSlot(slot);
        public T AtSlot(int slot) => m_List.AtSlot(slot);

        public GraphDataList<T>.Enumerator GetEnumerator() => m_List.GetEnumerator();
    }
}
