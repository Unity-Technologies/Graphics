namespace UnityEngine.Rendering.Universal
{
    public struct OutPt
    {
        Reference<OutPtStruct> m_Data;

        public void Initialize()
        {
            OutPtStruct initialValue = new OutPtStruct();
            Reference<OutPtStruct>.Create(initialValue, out m_Data);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool NotNull { get { return !m_Data.IsNull; } }
        public void SetNull() { m_Data.SetNull(); }
        public bool IsEqual(OutPt node) { return m_Data.IsEqual(node.m_Data); }
        public static bool operator ==(OutPt a, OutPt b) { return a.IsEqual(b); }
        public static bool operator !=(OutPt a, OutPt b) { return !a.IsEqual(b); }
        public override int GetHashCode() { return m_Data.GetHashCode(); }
        public override bool Equals(object obj) { return GetHashCode() == obj.GetHashCode(); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------

        internal ref int Idx { get { return ref m_Data.DeRef().Idx; } }
        internal ref IntPoint Pt { get { return ref m_Data.DeRef().Pt; } }
        internal ref OutPt Next { get { return ref m_Data.DeRef().Next; } }
        internal ref OutPt Prev { get { return ref m_Data.DeRef().Prev; } }
    }
}
