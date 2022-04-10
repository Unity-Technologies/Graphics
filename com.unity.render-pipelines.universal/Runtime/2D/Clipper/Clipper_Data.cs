using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;
    using Path = UnsafeList<IntPoint>;
    using Paths = UnsafeList<UnsafeList<IntPoint>>;

    internal partial struct Clipper
    {
        Reference<ClipperDataStruct> m_Data;
        TEdge NULL_TEdge;

        public static void Initialize(out Clipper clipper)
        {
            ClipperDataStruct initialValue = new ClipperDataStruct();
            Reference<ClipperDataStruct>.Create(initialValue, out clipper.m_Data);
            clipper.NULL_TEdge = new TEdge();
            clipper.m_edges = new UnsafeList<UnsafeList<TEdge>>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            clipper.m_PolyOuts = new UnsafeList<OutRec>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            clipper.m_IntersectList = new UnsafeList<IntersectNode>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            clipper.m_Joins = new UnsafeList<Join>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            clipper.m_GhostJoins = new UnsafeList<Join>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
        }

        public bool IsCreated { get { return m_Data.IsCreated; } }
        public bool IsNull { get { return m_Data.IsNull; } }

        //-----------------------------------------------------------------
        //                      Constant
        //-----------------------------------------------------------------
        internal const double horizontal = -3.4E+38;
        internal const int Skip = -2;
        internal const int Unassigned = -1;
        internal const double tolerance = 1.0E-20;

        public const ClipInt loRange = 0x3FFFFFFF;
        public const ClipInt hiRange = 0x3FFFFFFFFFFFFFFFL;

        public const int ioReverseSolution = 1;
        public const int ioStrictlySimple = 2;
        public const int ioPreserveCollinear = 4;

        //-----------------------------------------------------------------
        //                      Static
        //-----------------------------------------------------------------

        internal static bool near_zero(double val) { return (val > -tolerance) && (val < tolerance); }

        //-----------------------------------------------------------------
        //                      Properties
        //-----------------------------------------------------------------

        internal ref LocalMinima m_MinimaList { get { return ref m_Data.DeRef().m_MinimaList; } }
        internal ref LocalMinima m_CurrentLM { get { return ref m_Data.DeRef().m_CurrentLM; } }
        internal ref Scanbeam m_Scanbeam { get { return ref m_Data.DeRef().m_Scanbeam; } }
        internal ref UnsafeList<OutRec> m_PolyOuts { get { return ref m_Data.DeRef().m_PolyOuts; } }
        internal ref TEdge m_ActiveEdges { get { return ref m_Data.DeRef().m_ActiveEdges; } }
        internal ref bool m_UseFullRange { get { return ref m_Data.DeRef().m_UseFullRange; } }
        internal ref bool m_HasOpenPaths { get { return ref m_Data.DeRef().m_HasOpenPaths; } }
        internal ref bool PreserveCollinear { get { return ref m_Data.DeRef().PreserveCollinear; } }

        // Needs initialization..
        internal ref UnsafeList<UnsafeList<TEdge>> m_edges { get { return ref m_Data.DeRef().m_edges; } }

        // From Clipper
        //InitOptions that can be passed to the constructor ...
        internal ref ClipType m_ClipType { get { return ref m_Data.DeRef().m_ClipType; } }
        internal ref Maxima m_Maxima { get { return ref m_Data.DeRef().m_Maxima; } }
        internal ref TEdge m_SortedEdges { get { return ref m_Data.DeRef().m_SortedEdges; } }
        internal ref UnsafeList<IntersectNode> m_IntersectList { get { return ref m_Data.DeRef().m_IntersectList; } }
        internal ref MyIntersectNodeSort m_IntersectNodeComparer { get { return ref m_Data.DeRef().m_IntersectNodeComparer; } }
        internal ref bool m_ExecuteLocked { get { return ref m_Data.DeRef().m_ExecuteLocked; } }
        internal ref PolyFillType m_ClipFillType { get { return ref m_Data.DeRef().m_ClipFillType; } }
        internal ref PolyFillType m_SubjFillType { get { return ref m_Data.DeRef().m_SubjFillType; } }
        internal ref UnsafeList<Join> m_Joins { get { return ref m_Data.DeRef().m_Joins; } }
        internal ref UnsafeList<Join> m_GhostJoins { get { return ref m_Data.DeRef().m_GhostJoins; } }
        internal ref bool m_UsingPolyTree { get { return ref m_Data.DeRef().m_UsingPolyTree; } }

        internal ref int LastIndex { get { return ref m_Data.DeRef().LastIndex; } }
        internal ref bool ReverseSolution { get { return ref m_Data.DeRef().ReverseSolution; } }
        internal ref bool StrictlySimple { get { return ref m_Data.DeRef().StrictlySimple; } }
    }
}
