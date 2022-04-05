using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;
    using Path    = UnsafeList<IntPoint>;
    using Paths   = UnsafeList<UnsafeList<IntPoint>>;

    internal class TEdgeStruct
    {
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

    internal class IntersectNodeStruct
    {
        internal TEdge Edge1;
        internal TEdge Edge2;
        internal IntPoint Pt;
    };

    internal class LocalMinimaStruct
    {
        internal ClipInt Y;
        internal TEdge LeftBound;
        internal TEdge RightBound;
        internal LocalMinima Next;
    };

    internal class ScanbeamStruct
    {
        internal ClipInt Y;
        internal Scanbeam Next;
    };

    internal class MaximaStruct
    {
        internal ClipInt X;
        internal Maxima Next;
        internal Maxima Prev;
    };

    //OutRec: contains a path in the clipping solution. Edges in the AEL will
    //carry a pointer to an OutRec when they are part of the clipping solution.
    internal class OutRecStruct
    {
        internal int Idx;
        internal bool IsHole;
        internal bool IsOpen;
        internal OutRec FirstLeft; //see comments in clipper.pas
        internal OutPt Pts;
        internal OutPt BottomPt;
        internal PolyNode PolyNode;
    };

    internal class OutPtStruct
    {
        internal int Idx;
        internal IntPoint Pt;
        internal OutPt Next;
        internal OutPt Prev;
    };

    internal class JoinStruct
    {
        internal OutPt OutPt1;
        internal OutPt OutPt2;
        internal IntPoint OffPt;
    };

    internal class ClipperDataStruct
    {
        // From Clipper Base
        internal const double horizontal = -3.4E+38;
        internal const int Skip = -2;
        internal const int Unassigned = -1;
        internal const double tolerance = 1.0E-20;
        internal static bool near_zero(double val) { return (val > -tolerance) && (val < tolerance); }

        public const ClipInt loRange = 0x3FFFFFFF;
        public const ClipInt hiRange = 0x3FFFFFFFFFFFFFFFL;

        internal LocalMinima m_MinimaList;
        internal LocalMinima m_CurrentLM;
        internal List<List<TEdge>> m_edges = new List<List<TEdge>>();
        internal Scanbeam m_Scanbeam;
        internal List<OutRec> m_PolyOuts;
        internal TEdge m_ActiveEdges;
        internal bool m_UseFullRange;
        internal bool m_HasOpenPaths;
        internal bool PreserveCollinear;

        // From Clipper
        //InitOptions that can be passed to the constructor ...
        public const int ioReverseSolution = 1;
        public const int ioStrictlySimple = 2;
        public const int ioPreserveCollinear = 4;

        internal ClipType m_ClipType;
        internal Maxima m_Maxima;
        internal TEdge m_SortedEdges;
        internal List<IntersectNode> m_IntersectList;
        internal IComparer<IntersectNode> m_IntersectNodeComparer;
        internal bool m_ExecuteLocked;
        internal PolyFillType m_ClipFillType;
        internal PolyFillType m_SubjFillType;
        internal List<Join> m_Joins;
        internal List<Join> m_GhostJoins;
        internal bool m_UsingPolyTree;

        internal int LastIndex;
        internal bool ReverseSolution;
        internal bool StrictlySimple;
    }
}
