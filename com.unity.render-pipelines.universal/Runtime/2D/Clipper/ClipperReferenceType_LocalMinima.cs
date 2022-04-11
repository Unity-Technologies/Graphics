using System;

namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;

    internal struct LocalMinima
    {
        static int CurrentID;

        Reference<LocalMinimaStruct> m_Data;

        public void Initialize()
        {
            LocalMinimaStruct initialValue = new LocalMinimaStruct();
            initialValue.Id = CurrentID;
            CurrentID++;
            Reference<LocalMinimaStruct>.Create(initialValue, out m_Data);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool NotNull { get { return !m_Data.IsNull; } }
        public void SetNull() { m_Data.SetNull(); }
        public bool IsEqual(LocalMinima node) { return m_Data.IsEqual(node.m_Data); }
        
        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------
        public ref  int Id { get { return ref m_Data.DeRef().Id; } }
        public ref ClipInt Y { get { return ref m_Data.DeRef().Y; }}

        public ref TEdge LeftBound { get { return ref m_Data.DeRef().LeftBound; }}

        public ref TEdge RightBound { get { return ref m_Data.DeRef().RightBound; }}

        public ref LocalMinima Next { get { return ref m_Data.DeRef().Next; }}
    }
}
