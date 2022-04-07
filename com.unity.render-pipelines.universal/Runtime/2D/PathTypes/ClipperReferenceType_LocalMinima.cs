using System;

namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;

    internal struct LocalMinima
    {
        Reference<LocalMinimaStruct> m_Data;

        public void Initialize()
        {
            LocalMinimaStruct initialValue = new LocalMinimaStruct();
            m_Data = Reference<LocalMinimaStruct>.Create(initialValue);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool IsEqual(LocalMinima node) { return m_Data.IsEqual(node.m_Data); }
        public void Clear() { m_Data.Clear(); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------
        public ref ClipInt Y { get { return ref m_Data.DeRef().Y; }}

        public ref TEdge LeftBound { get { return ref m_Data.DeRef().LeftBound; }}

        public ref TEdge RightBound { get { return ref m_Data.DeRef().RightBound; }}

        public ref LocalMinima Next { get { return ref m_Data.DeRef().Next; }}
    }
}
