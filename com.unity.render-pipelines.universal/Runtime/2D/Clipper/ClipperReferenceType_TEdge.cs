using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    internal struct TEdge
    {
        static int CurrentId;

        Reference<TEdgeStruct> m_Data;

        public void Initialize()
        {
            TEdgeStruct initialValue = new TEdgeStruct();
            initialValue.Id = CurrentId;
            CurrentId++;
            Reference<TEdgeStruct>.Create(initialValue, out m_Data);
        }

        public ref int Id { get { return ref m_Data.DeRef().Id; }  }
        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }
        public bool NotNull { get { return !m_Data.IsNull; } }
        public void SetNull() { m_Data.SetNull(); }
        public bool IsEqual(TEdge node) { return m_Data.IsEqual(node.m_Data); }
        public static bool operator ==(TEdge a, TEdge b) { return a.IsEqual(b); }
        public static bool operator !=(TEdge a, TEdge b) { return !a.IsEqual(b); }
        public override int GetHashCode() { return m_Data.GetHashCode(); }
        public override bool Equals(object obj) { return GetHashCode() == obj.GetHashCode(); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------

        public ref IntPoint Bot { get { return ref m_Data.DeRef().Bot; }}

        //current (updated for every new scanbeam)
        public ref IntPoint Curr { get { return ref m_Data.DeRef().Curr; }}

        public ref IntPoint Top { get { return ref m_Data.DeRef().Top; }}

        public ref IntPoint Delta { get { return ref m_Data.DeRef().Delta; }}

        public ref double Dx { get { return ref m_Data.DeRef().Dx; }}

        public ref PolyType PolyTyp { get { return ref m_Data.DeRef().PolyTyp; }}

        //side only refers to current side of solution poly
        public ref EdgeSide Side { get { return ref m_Data.DeRef().Side; }}

        //1 or -1 depending on winding direction
        public ref int WindDelta { get { return ref m_Data.DeRef().WindDelta; }}

        public ref int WindCnt { get { return ref m_Data.DeRef().WindCnt; }}

        //winding count of the opposite polytype
        public ref int WindCnt2  { get { return ref m_Data.DeRef().WindCnt2; }}

        public ref int OutIdx { get { return ref m_Data.DeRef().OutIdx; }}
        public ref TEdge Next { get { return ref m_Data.DeRef().Next; }}

        public ref TEdge Prev { get { return ref m_Data.DeRef().Prev; }}

        public ref TEdge NextInLML { get { return ref m_Data.DeRef().NextInLML; }}

        public ref TEdge NextInAEL { get { return ref m_Data.DeRef().NextInAEL; }}

        public ref TEdge PrevInAEL { get { return ref m_Data.DeRef().PrevInAEL; }}

        public ref TEdge NextInSEL { get { return ref m_Data.DeRef().NextInSEL; }}

        public ref TEdge PrevInSEL { get { return ref m_Data.DeRef().PrevInSEL; }}
    };
}
