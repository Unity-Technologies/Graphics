using System;

namespace UnityEngine.Rendering.Universal
{
    internal struct IntersectNode 
    {
        Reference<IntersectNodeStruct> m_Data;

        public void Initialize()
        {
            IntersectNodeStruct initialValue = new IntersectNodeStruct();
            m_Data = Reference<IntersectNodeStruct>.Create(initialValue);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool IsEqual(IntersectNode node) { return m_Data.IsEqual(node.m_Data); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------
        public ref TEdge Edge1 { get { return ref m_Data.DeRef().Edge1; }}

        public ref TEdge Edge2 { get { return ref m_Data.DeRef().Edge2; }}

        public ref IntPoint Pt { get { return ref m_Data.DeRef().Pt; }}

    }
}

