using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;
    using Path = UnsafeList<IntPoint>;
    using Paths = UnsafeList<UnsafeList<IntPoint>>;

    internal struct TEdgeStruct
    {
        internal int Id;  // This is for debugging...
        internal IntPoint Bot;
        internal IntPoint Curr; //current (updated for every new scanbeam)
        internal IntPoint Top;
        internal IntPoint Delta;
        internal double Dx;
        internal PolyType PolyTyp;
        internal EdgeSide Side; //side only refers to current side of solution poly
        internal int WindDelta; //1 or -1 depending on winding direction
        internal int WindCnt;
        internal int WindCnt2; //winding count of the opposite polytype
        internal int OutIdx;
        internal TEdge Next;
        internal TEdge Prev;
        internal TEdge NextInLML;
        internal TEdge NextInAEL;
        internal TEdge PrevInAEL;
        internal TEdge NextInSEL;
        internal TEdge PrevInSEL;
    };

    internal struct IntersectNodeStruct
    {
        internal TEdge Edge1;
        internal TEdge Edge2;
        internal IntPoint Pt;
    };

    internal struct LocalMinimaStruct
    {
        internal int Id;
        internal ClipInt Y;
        internal TEdge LeftBound;
        internal TEdge RightBound;
        internal LocalMinima Next;
    };

    internal struct ScanbeamStruct
    {
        internal ClipInt Y;
        internal Scanbeam Next;
    };

    internal struct MaximaStruct
    {
        internal ClipInt X;
        internal Maxima Next;
        internal Maxima Prev;
    };

    //OutRec: contains a path in the clipping solution. Edges in the AEL will
    //carry a pointer to an OutRec when they are part of the clipping solution.
    internal struct OutRecStruct
    {
        internal int Idx;
        internal bool IsHole;
        internal bool IsOpen;
        internal OutRec FirstLeft; //see comments in clipper.pas
        internal OutPt Pts;
        internal OutPt BottomPt;
        internal PolyNode PolyNode;
    };

    internal struct OutPtStruct
    {
        internal int Idx;
        internal IntPoint Pt;
        internal OutPt Next;
        internal OutPt Prev;
    };

    internal struct JoinStruct
    {
        internal OutPt OutPt1;
        internal OutPt OutPt2;
        internal IntPoint OffPt;
    };

    internal struct ClipperDataStruct
    {
        internal LocalMinima m_MinimaList;
        internal LocalMinima m_CurrentLM;
        internal UnsafeList<UnsafeList<TEdge>> m_edges; // = new List<List<TEdge>>();
        internal Scanbeam m_Scanbeam;
        internal UnsafeList<OutRec> m_PolyOuts;
        internal TEdge m_ActiveEdges;
        internal bool m_UseFullRange;
        internal bool m_HasOpenPaths;
        internal bool PreserveCollinear;

        internal ClipType m_ClipType;
        internal Maxima m_Maxima;
        internal TEdge m_SortedEdges;
        internal UnsafeList<IntersectNode> m_IntersectList;

        internal MyIntersectNodeSort m_IntersectNodeComparer;

        internal bool m_ExecuteLocked;
        internal PolyFillType m_ClipFillType;
        internal PolyFillType m_SubjFillType;
        internal UnsafeList<Join> m_Joins;
        internal UnsafeList<Join> m_GhostJoins;
        internal bool m_UsingPolyTree;

        internal int LastIndex;
        internal bool ReverseSolution;
        internal bool StrictlySimple;
    }
}
