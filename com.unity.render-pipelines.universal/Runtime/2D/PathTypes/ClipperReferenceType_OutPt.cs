namespace UnityEngine.Rendering.Universal
{
    public struct OutPt
    {
        Reference<OutPtStruct> m_Data;

        public void Initialize()
        {
            OutPtStruct initialValue = new OutPtStruct();
            m_Data = Reference<OutPtStruct>.Create(initialValue);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool IsEqual(OutPt node) { return m_Data.IsEqual(node.m_Data); }
        public void Clear() { m_Data.Clear(); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------

        internal ref int Idx { get { return ref m_Data.DeRef().Idx; } }
        internal ref IntPoint Pt { get { return ref m_Data.DeRef().Pt; } }
        internal ref OutPt Next { get { return ref m_Data.DeRef().Next; } }
        internal ref OutPt Prev { get { return ref m_Data.DeRef().Prev; } }
    }
}
