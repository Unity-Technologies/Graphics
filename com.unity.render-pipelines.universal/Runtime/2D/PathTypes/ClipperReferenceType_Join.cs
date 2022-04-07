namespace UnityEngine.Rendering.Universal
{
    public struct Join
    {
        Reference<JoinStruct> m_Data;

        public void Initialize()
        {
            JoinStruct initialValue = new JoinStruct();
            m_Data = Reference<JoinStruct>.Create(initialValue);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool NotNull { get { return !m_Data.IsNull; } }
        public void SetNull() { m_Data.SetNull(); }
        public bool IsEqual(Join node) { return m_Data.IsEqual(node.m_Data); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------

        internal ref OutPt OutPt1 { get { return ref m_Data.DeRef().OutPt1; } }
        internal ref OutPt OutPt2 { get { return ref m_Data.DeRef().OutPt2; } }
        internal ref IntPoint OffPt { get { return ref m_Data.DeRef().OffPt; } }
    }
}
