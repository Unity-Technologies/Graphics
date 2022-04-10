using System;

namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;

    public struct Scanbeam
    {
        Reference<ScanbeamStruct> m_Data;

        public void Initialize()
        {
            ScanbeamStruct initialValue = new ScanbeamStruct();
            Reference<ScanbeamStruct>.Create(initialValue, out m_Data);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool NotNull { get { return !m_Data.IsNull; } }
        public void SetNull() { m_Data.SetNull(); }
        public bool IsEqual(Scanbeam node) { return m_Data.IsEqual(node.m_Data); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------
        internal ref ClipInt Y { get { return ref m_Data.DeRef().Y; }}
        internal ref Scanbeam Next { get { return ref m_Data.DeRef().Next; }}
    }
}
