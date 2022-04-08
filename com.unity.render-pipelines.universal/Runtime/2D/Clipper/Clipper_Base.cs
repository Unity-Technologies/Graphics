using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal
{
    using ClipInt = Int64;
    using Path = UnsafeList<IntPoint>;
    using Paths = UnsafeList<UnsafeList<IntPoint>>;

    // This code was originally in the ClipperBase class
    internal partial struct Clipper
    {
        //------------------------------------------------------------------------------
        // Clipper Base
        //------------------------------------------------------------------------------

        public void Swap(ref ClipInt val1, ref ClipInt val2)
        {
            ClipInt tmp = val1;
            val1 = val2;
            val2 = tmp;
        }

        //------------------------------------------------------------------------------

        internal static bool IsHorizontal(ref TEdge e)
        {
            return e.Delta.Y == 0;
        }

        //------------------------------------------------------------------------------

        internal bool PointIsVertex(ref IntPoint pt, ref OutPt pp)
        {
            OutPt pp2 = pp;
            do
            {
                if (pp2.Pt == pt) return true;
                pp2 = pp2.Next;
            }
            while (pp2 != pp);
            return false;
        }

        //------------------------------------------------------------------------------

        internal bool PointOnLineSegment(ref IntPoint pt,
            ref IntPoint linePt1, ref IntPoint linePt2, bool UseFullRange)
        {
            if (UseFullRange)
                return ((pt.X == linePt1.X) && (pt.Y == linePt1.Y)) ||
                    ((pt.X == linePt2.X) && (pt.Y == linePt2.Y)) ||
                    (((pt.X > linePt1.X) == (pt.X < linePt2.X)) &&
                        ((pt.Y > linePt1.Y) == (pt.Y < linePt2.Y)) &&
                        ((Int128.Int128Mul((pt.X - linePt1.X), (linePt2.Y - linePt1.Y)) ==
                            Int128.Int128Mul((linePt2.X - linePt1.X), (pt.Y - linePt1.Y)))));
            else
                return ((pt.X == linePt1.X) && (pt.Y == linePt1.Y)) ||
                    ((pt.X == linePt2.X) && (pt.Y == linePt2.Y)) ||
                    (((pt.X > linePt1.X) == (pt.X < linePt2.X)) &&
                        ((pt.Y > linePt1.Y) == (pt.Y < linePt2.Y)) &&
                        ((pt.X - linePt1.X) * (linePt2.Y - linePt1.Y) ==
                            (linePt2.X - linePt1.X) * (pt.Y - linePt1.Y)));
        }

        //------------------------------------------------------------------------------

        internal bool PointOnPolygon(ref IntPoint pt, ref OutPt pp, bool UseFullRange)
        {
            OutPt pp2 = pp;
            while (true)
            {
                if (PointOnLineSegment(ref pt, ref pp2.Pt, ref pp2.Next.Pt, UseFullRange))
                    return true;
                pp2 = pp2.Next;
                if (pp2 == pp) break;
            }
            return false;
        }

        //------------------------------------------------------------------------------

        internal static bool SlopesEqual(ref TEdge e1, ref TEdge e2, bool UseFullRange)
        {
            if (UseFullRange)
                return Int128.Int128Mul(e1.Delta.Y, e2.Delta.X) ==
                    Int128.Int128Mul(e1.Delta.X, e2.Delta.Y);
            else
                return (ClipInt)(e1.Delta.Y) * (e2.Delta.X) ==
                    (ClipInt)(e1.Delta.X) * (e2.Delta.Y);
        }

        //------------------------------------------------------------------------------

        internal static bool SlopesEqual(IntPoint pt1, IntPoint pt2,
            IntPoint pt3, bool UseFullRange)
        {
            if (UseFullRange)
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt2.X - pt3.X) ==
                    Int128.Int128Mul(pt1.X - pt2.X, pt2.Y - pt3.Y);
            else
                return
                    (ClipInt)(pt1.Y - pt2.Y) * (pt2.X - pt3.X) - (ClipInt)(pt1.X - pt2.X) * (pt2.Y - pt3.Y) == 0;
        }

        //------------------------------------------------------------------------------

        internal static bool SlopesEqual(IntPoint pt1, IntPoint pt2,
            IntPoint pt3, IntPoint pt4, bool UseFullRange)
        {
            if (UseFullRange)
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt3.X - pt4.X) ==
                    Int128.Int128Mul(pt1.X - pt2.X, pt3.Y - pt4.Y);
            else
                return
                    (ClipInt)(pt1.Y - pt2.Y) * (pt3.X - pt4.X) - (ClipInt)(pt1.X - pt2.X) * (pt3.Y - pt4.Y) == 0;
        }


        //------------------------------------------------------------------------------

        public void Clear()
        {
            DisposeLocalMinimaList();
            for (int i = 0; i < m_edges.Length; ++i)
            {
                for (int j = 0; j < m_edges[i].Length; ++j) m_edges[i][j].SetNull();
                m_edges[i].Clear();
            }
            m_edges.Clear();
            m_UseFullRange = false;
            m_HasOpenPaths = false;
        }

        //------------------------------------------------------------------------------

        private void DisposeLocalMinimaList()
        {
            while (m_MinimaList.NotNull)
            {
                LocalMinima tmpLm = m_MinimaList.Next;
                m_MinimaList.SetNull();
                m_MinimaList = tmpLm;
            }
            m_CurrentLM.SetNull();
        }

        //------------------------------------------------------------------------------

        void RangeTest(ref IntPoint Pt, ref bool useFullRange)
        {
            if (useFullRange)
            {
                if (Pt.X > hiRange || Pt.Y > hiRange || -Pt.X > hiRange || -Pt.Y > hiRange)
                    throw new ClipperException("Coordinate outside allowed range");
            }
            else if (Pt.X > loRange || Pt.Y > loRange || -Pt.X > loRange || -Pt.Y > loRange)
            {
                useFullRange = true;
                RangeTest(ref Pt, ref useFullRange);
            }
        }

        //------------------------------------------------------------------------------

        private void InitEdge(ref TEdge e, ref TEdge eNext,
            ref TEdge ePrev, ref IntPoint pt)
        {
            e.Next = eNext;
            e.Prev = ePrev;
            e.Curr = pt;
            e.OutIdx = Unassigned;
        }

        //------------------------------------------------------------------------------

        private void InitEdge2(ref TEdge e, PolyType polyType)
        {
            if (e.Curr.Y >= e.Next.Curr.Y)
            {
                e.Bot = e.Curr;
                e.Top = e.Next.Curr;
            }
            else
            {
                e.Top = e.Curr;
                e.Bot = e.Next.Curr;
            }
            SetDx(ref e);
            e.PolyTyp = polyType;
        }

        //------------------------------------------------------------------------------

        private TEdge FindNextLocMin(ref TEdge E)
        {
            TEdge E2;
            for (; ; )
            {
                while (E.Bot != E.Prev.Bot || E.Curr == E.Top) E = E.Next;
                if (E.Dx != horizontal && E.Prev.Dx != horizontal) break;
                while (E.Prev.Dx == horizontal) E = E.Prev;
                E2 = E;
                while (E.Dx == horizontal) E = E.Next;
                if (E.Top.Y == E.Prev.Bot.Y) continue; //ie just an intermediate horz.
                if (E2.Prev.Bot.X < E.Bot.X) E = E2;
                break;
            }
            return E;
        }

        //------------------------------------------------------------------------------

        private TEdge ProcessBound(ref TEdge E, bool LeftBoundIsForward)
        {
            TEdge EStart, Result = E;
            TEdge Horz;

            if (Result.OutIdx == Skip)
            {
                //check if there are edges beyond the skip edge in the bound and if so
                //create another LocMin and calling ProcessBound once more ...
                E = Result;
                if (LeftBoundIsForward)
                {
                    while (E.Top.Y == E.Next.Bot.Y) E = E.Next;
                    while (E != Result && E.Dx == horizontal) E = E.Prev;
                }
                else
                {
                    while (E.Top.Y == E.Prev.Bot.Y) E = E.Prev;
                    while (E != Result && E.Dx == horizontal) E = E.Next;
                }
                if (E == Result)
                {
                    if (LeftBoundIsForward) Result = E.Next;
                    else Result = E.Prev;
                }
                else
                {
                    //there are more edges in the bound beyond result starting with E
                    if (LeftBoundIsForward)
                        E = Result.Next;
                    else
                        E = Result.Prev;
                    LocalMinima locMin = new LocalMinima();
                    locMin.Initialize();
                    locMin.Next.SetNull();
                    locMin.Y = E.Bot.Y;
                    locMin.LeftBound.SetNull();
                    locMin.RightBound = E;
                    E.WindDelta = 0;
                    Result = ProcessBound(ref E, LeftBoundIsForward);
                    InsertLocalMinima(ref locMin);
                }
                return Result;
            }

            if (E.Dx == horizontal)
            {
                //We need to be careful with open paths because this may not be a
                //true local minima (ie E may be following a skip edge).
                //Also, consecutive horz. edges may start heading left before going right.
                if (LeftBoundIsForward) EStart = E.Prev;
                else EStart = E.Next;
                if (EStart.Dx == horizontal) //ie an adjoining horizontal skip edge
                {
                    if (EStart.Bot.X != E.Bot.X && EStart.Top.X != E.Bot.X)
                        ReverseHorizontal(ref E);
                }
                else if (EStart.Bot.X != E.Bot.X)
                    ReverseHorizontal(ref E);
            }

            EStart = E;
            if (LeftBoundIsForward)
            {
                while (Result.Top.Y == Result.Next.Bot.Y && Result.Next.OutIdx != Skip)
                    Result = Result.Next;
                if (Result.Dx == horizontal && Result.Next.OutIdx != Skip)
                {
                    //nb: at the top of a bound, horizontals are added to the bound
                    //only when the preceding edge attaches to the horizontal's left vertex
                    //unless a Skip edge is encountered when that becomes the top divide
                    Horz = Result;
                    while (Horz.Prev.Dx == horizontal) Horz = Horz.Prev;
                    if (Horz.Prev.Top.X > Result.Next.Top.X) Result = Horz.Prev;
                }
                while (E != Result)
                {
                    E.NextInLML = E.Next;
                    if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Prev.Top.X)
                        ReverseHorizontal(ref E);
                    E = E.Next;
                }
                if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Prev.Top.X)
                    ReverseHorizontal(ref E);
                Result = Result.Next; //move to the edge just beyond current bound
            }
            else
            {
                while (Result.Top.Y == Result.Prev.Bot.Y && Result.Prev.OutIdx != Skip)
                    Result = Result.Prev;
                if (Result.Dx == horizontal && Result.Prev.OutIdx != Skip)
                {
                    Horz = Result;
                    while (Horz.Next.Dx == horizontal) Horz = Horz.Next;
                    if (Horz.Next.Top.X == Result.Prev.Top.X ||
                        Horz.Next.Top.X > Result.Prev.Top.X) Result = Horz.Next;
                }

                while (E != Result)
                {
                    E.NextInLML = E.Prev;
                    if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Next.Top.X)
                        ReverseHorizontal(ref E);
                    E = E.Prev;
                }
                if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Next.Top.X)
                    ReverseHorizontal(ref E);
                Result = Result.Prev; //move to the edge just beyond current bound
            }
            return Result;
        }

        //------------------------------------------------------------------------------


        public bool AddPath(ref Path pg, PolyType polyType, bool Closed)
        {
#if use_lines
                if (!Closed && polyType == PolyType.ptClip)
                    throw new ClipperException("AddPath: Open paths must be subject.");
#else
            if (!Closed)
                throw new ClipperException("AddPath: Open paths have been disabled.");
#endif

            int highI = (int)pg.Length - 1;
            if (Closed) while (highI > 0 && (pg[highI] == pg[0])) --highI;
            while (highI > 0 && (pg[highI] == pg[highI - 1])) --highI;
            if ((Closed && highI < 2) || (!Closed && highI < 1)) return false;

            //create a new edge array ...
            UnsafeList<TEdge> edges = new UnsafeList<TEdge>(highI + 1, Allocator.Temp, Unity.Collections.NativeArrayOptions.ClearMemory);
            for (int i = 0; i <= highI; i++)
            {
                TEdge edge = new TEdge();
                edge.Initialize();
                edges.Add(edge);
            }

            bool IsFlat = true;

            //1. Basic (first) edge initialization ...
            edges[1].Curr = pg[1];
            RangeTest(ref pg.GetIndexByRef(0), ref m_UseFullRange);
            RangeTest(ref pg.GetIndexByRef(highI), ref m_UseFullRange);
            InitEdge(ref edges.GetIndexByRef(0), ref edges.GetIndexByRef(1), ref edges.GetIndexByRef(highI), ref pg.GetIndexByRef(0));
            InitEdge(ref edges.GetIndexByRef(highI), ref edges.GetIndexByRef(0), ref edges.GetIndexByRef(highI-1), ref pg.GetIndexByRef(highI));
            for (int i = highI - 1; i >= 1; --i)
            {
                RangeTest(ref pg.GetIndexByRef(i), ref m_UseFullRange);
                InitEdge(ref edges.GetIndexByRef(i), ref edges.GetIndexByRef(i+1), ref edges.GetIndexByRef(i-1), ref pg.GetIndexByRef(i));
            }
            TEdge eStart = edges[0];

            //2. Remove duplicate vertices, and (when closed) collinear edges ...
            TEdge E = eStart, eLoopStop = eStart;
            for (; ; )
            {
                //nb: allows matching start and end points when not Closed ...
                if (E.Curr == E.Next.Curr && (Closed || E.Next != eStart))
                {
                    if (E == E.Next) break;
                    if (E == eStart) eStart = E.Next;
                    E = RemoveEdge(ref E);
                    eLoopStop = E;
                    continue;
                }
                if (E.Prev == E.Next)
                    break; //only two vertices
                else if (Closed &&
                         SlopesEqual(E.Prev.Curr, E.Curr, E.Next.Curr, m_UseFullRange) &&
                         (!PreserveCollinear ||
                          !Pt2IsBetweenPt1AndPt3(E.Prev.Curr, E.Curr, E.Next.Curr)))
                {
                    //Collinear edges are allowed for open paths but in closed paths
                    //the default is to merge adjacent collinear edges into a single edge.
                    //However, if the PreserveCollinear property is enabled, only overlapping
                    //collinear edges (ie spikes) will be removed from closed paths.
                    if (E == eStart) eStart = E.Next;
                    E = RemoveEdge(ref E);
                    E = E.Prev;
                    eLoopStop = E;
                    continue;
                }
                E = E.Next;
                if ((E == eLoopStop) || (!Closed && E.Next == eStart)) break;
            }

            if ((!Closed && (E == E.Next)) || (Closed && (E.Prev == E.Next)))
                return false;

            if (!Closed)
            {
                m_HasOpenPaths = true;
                eStart.Prev.OutIdx = Skip;
            }

            //3. Do second stage of edge initialization ...
            E = eStart;
            do
            {
                InitEdge2(ref E, polyType);
                E = E.Next;
                if (IsFlat && E.Curr.Y != eStart.Curr.Y) IsFlat = false;
            }
            while (E != eStart);

            //4. Finally, add edge bounds to LocalMinima list ...

            //Totally flat paths must be handled differently when adding them
            //to LocalMinima list to avoid endless loops etc ...
            if (IsFlat)
            {
                if (Closed) return false;
                E.Prev.OutIdx = Skip;
                LocalMinima locMin = new LocalMinima();
                locMin.Initialize();
                locMin.Next.SetNull();
                locMin.Y = E.Bot.Y;
                locMin.LeftBound.SetNull();
                locMin.RightBound = E;
                locMin.RightBound.Side = EdgeSide.esRight;
                locMin.RightBound.WindDelta = 0;
                for (; ; )
                {
                    if (E.Bot.X != E.Prev.Top.X) ReverseHorizontal(ref E);
                    if (E.Next.OutIdx == Skip) break;
                    E.NextInLML = E.Next;
                    E = E.Next;
                }
                InsertLocalMinima(ref locMin);
                m_edges.Add(edges);
                return true;
            }

            m_edges.Add(edges);
            bool leftBoundIsForward;
            TEdge EMin = new TEdge();
            EMin.Initialize();

            //workaround to avoid an endless loop in the while loop below when
            //open paths have matching start and end points ...
            if (E.Prev.Bot == E.Prev.Top) E = E.Next;

            for (; ; )
            {
                E = FindNextLocMin(ref E);
                    if (E == EMin) break;
                else if (EMin.IsNull) EMin = E;

                //E and E.Prev now share a local minima (left aligned if horizontal).
                //Compare their slopes to find which starts which bound ...
                LocalMinima locMin = new LocalMinima();
                locMin.Initialize();
                locMin.Next.SetNull();
                locMin.Y = E.Bot.Y;
                if (E.Dx < E.Prev.Dx)
                {
                    locMin.LeftBound = E.Prev;
                    locMin.RightBound = E;
                    leftBoundIsForward = false; //Q.nextInLML = Q.prev
                }
                else
                {
                    locMin.LeftBound = E;
                    locMin.RightBound = E.Prev;
                    leftBoundIsForward = true; //Q.nextInLML = Q.next
                }
                locMin.LeftBound.Side = EdgeSide.esLeft;
                locMin.RightBound.Side = EdgeSide.esRight;

                if (!Closed) locMin.LeftBound.WindDelta = 0;
                else if (locMin.LeftBound.Next == locMin.RightBound)
                    locMin.LeftBound.WindDelta = -1;
                else locMin.LeftBound.WindDelta = 1;
                locMin.RightBound.WindDelta = -locMin.LeftBound.WindDelta;

                E = ProcessBound(ref locMin.LeftBound, leftBoundIsForward);
                if (E.OutIdx == Skip) E = ProcessBound(ref E, leftBoundIsForward);

                TEdge E2 = ProcessBound(ref locMin.RightBound, !leftBoundIsForward);
                if (E2.OutIdx == Skip) E2 = ProcessBound(ref E2, !leftBoundIsForward);

                if (locMin.LeftBound.OutIdx == Skip)
                    locMin.LeftBound.SetNull();
                else if (locMin.RightBound.OutIdx == Skip)
                    locMin.RightBound.SetNull();
                InsertLocalMinima(ref locMin);
                if (!leftBoundIsForward) E = E2;
            }
            return true;
        }

        //------------------------------------------------------------------------------

        public bool AddPaths(ref Paths ppg, PolyType polyType, bool closed)
        {
            bool result = false;
            for (int i = 0; i < ppg.Length; ++i)
                if (AddPath(ref ppg.GetIndexByRef(i), polyType, closed)) result = true;
            return result;
        }

        //------------------------------------------------------------------------------

        internal bool Pt2IsBetweenPt1AndPt3(IntPoint pt1, IntPoint pt2, IntPoint pt3)
        {
            if ((pt1 == pt3) || (pt1 == pt2) || (pt3 == pt2)) return false;
            else if (pt1.X != pt3.X) return (pt2.X > pt1.X) == (pt2.X < pt3.X);
            else return (pt2.Y > pt1.Y) == (pt2.Y < pt3.Y);
        }

        //------------------------------------------------------------------------------

        TEdge RemoveEdge(ref TEdge e)
        {
            //removes e from double_linked_list (but without removing from memory)
            e.Prev.Next = e.Next;
            e.Next.Prev = e.Prev;
            TEdge result = e.Next;
            e.Prev.SetNull(); //flag as removed (see ClipperBase.Clear)
            return result;
        }

        //------------------------------------------------------------------------------

        private void SetDx(ref TEdge e)
        {
            e.Delta.X = (e.Top.X - e.Bot.X);
            e.Delta.Y = (e.Top.Y - e.Bot.Y);
            if (e.Delta.Y == 0) e.Dx = horizontal;
            else e.Dx = (double)(e.Delta.X) / (e.Delta.Y);
        }

        //---------------------------------------------------------------------------

        private void InsertLocalMinima(ref LocalMinima newLm)
        {
            if (m_MinimaList.IsNull)
            {
                m_MinimaList = newLm;
            }
            else if (newLm.Y >= m_MinimaList.Y)
            {
                newLm.Next = m_MinimaList;
                m_MinimaList = newLm;
            }
            else
            {
                LocalMinima tmpLm = m_MinimaList;
                while (tmpLm.Next.NotNull && (newLm.Y < tmpLm.Next.Y))
                    tmpLm = tmpLm.Next;
                newLm.Next = tmpLm.Next;
                tmpLm.Next = newLm;
            }
        }

        //------------------------------------------------------------------------------

        internal Boolean PopLocalMinima(ref ClipInt Y, out LocalMinima current)
        {
            current = m_CurrentLM;
            if (m_CurrentLM.NotNull && m_CurrentLM.Y == Y)
            {
                m_CurrentLM = m_CurrentLM.Next;
                return true;
            }
            return false;
        }

        //------------------------------------------------------------------------------

        private void ReverseHorizontal(ref TEdge e)
        {
            //swap horizontal edges' top and bottom x's so they follow the natural
            //progression of the bounds - ie so their xbots will align with the
            //adjoining lower edge. [Helpful in the ProcessHorizontal() method.]
            Swap(ref e.Top.X, ref e.Bot.X);
        }

        //------------------------------------------------------------------------------

        internal void Reset()
        {
            m_CurrentLM = m_MinimaList;
            if (m_CurrentLM.IsNull) return; //ie nothing to process

            //reset all edges ...
            m_Scanbeam.SetNull();
            LocalMinima lm = m_MinimaList;
            while (lm.NotNull)
            {
                InsertScanbeam(ref lm.Y);
                TEdge e = lm.LeftBound;
                if (e.NotNull)
                {
                    e.Curr = e.Bot;
                    e.OutIdx = Unassigned;
                }
                e = lm.RightBound;
                if (e.NotNull)
                {
                    e.Curr = e.Bot;
                    e.OutIdx = Unassigned;
                }
                lm = lm.Next;
            }
            m_ActiveEdges.SetNull();
        }

        //------------------------------------------------------------------------------

        public static IntRect GetBounds(ref Paths paths)
        {
            int i = 0, cnt = paths.Length;
            while (i < cnt && paths[i].Length == 0) i++;
            if (i == cnt) return new IntRect(0, 0, 0, 0);
            IntRect result = new IntRect();
            result.left = paths[i][0].X;
            result.right = result.left;
            result.top = paths[i][0].Y;
            result.bottom = result.top;
            for (; i < cnt; i++)
                for (int j = 0; j < paths[i].Length; j++)
                {
                    if (paths[i][j].X < result.left) result.left = paths[i][j].X;
                    else if (paths[i][j].X > result.right) result.right = paths[i][j].X;
                    if (paths[i][j].Y < result.top) result.top = paths[i][j].Y;
                    else if (paths[i][j].Y > result.bottom) result.bottom = paths[i][j].Y;
                }
            return result;
        }

        //------------------------------------------------------------------------------

        internal void InsertScanbeam(ref ClipInt Y)
        {
            //single-linked list: sorted descending, ignoring dups.
            if (m_Scanbeam.IsNull)
            {
                m_Scanbeam = new Scanbeam();
                m_Scanbeam.Initialize();
                m_Scanbeam.Next.SetNull();
                m_Scanbeam.Y = Y;
            }
            else if (Y > m_Scanbeam.Y)
            {
                Scanbeam newSb = new Scanbeam();
                newSb.Initialize();
                newSb.Y = Y;
                newSb.Next = m_Scanbeam;
                m_Scanbeam = newSb;
            }
            else
            {
                Scanbeam sb2 = m_Scanbeam;
                while (sb2.Next.NotNull && (Y <= sb2.Next.Y)) sb2 = sb2.Next;
                if (Y == sb2.Y) return; //ie ignores duplicates
                Scanbeam newSb = new Scanbeam();
                newSb.Initialize();
                newSb.Y = Y;
                newSb.Next = sb2.Next;
                sb2.Next = newSb;
            }
        }

        //------------------------------------------------------------------------------

        internal Boolean PopScanbeam(out ClipInt Y)
        {
            if (m_Scanbeam.IsNull)
            {
                Y = 0;
                return false;
            }
            Y = m_Scanbeam.Y;
            m_Scanbeam = m_Scanbeam.Next;
            return true;
        }

        //------------------------------------------------------------------------------

        internal Boolean LocalMinimaPending()
        {
            return (m_CurrentLM.NotNull);
        }

        //------------------------------------------------------------------------------

        internal OutRec CreateOutRec()
        {
            OutRec result = new OutRec();
            result.Initialize();
            result.Idx = Unassigned;
            result.IsHole = false;
            result.IsOpen = false;
            result.FirstLeft.SetNull();
            result.Pts.SetNull();
            result.BottomPt.SetNull();
            result.PolyNode = default(PolyNode);
            m_PolyOuts.Add(result);
            result.Idx = m_PolyOuts.Length - 1;
            return result;
        }

        //------------------------------------------------------------------------------

        internal void DisposeOutRec(int index)
        {
            OutRec outRec = m_PolyOuts[index];
            outRec.Pts.SetNull();
            outRec.SetNull();
            m_PolyOuts[index].SetNull();
        }

        //------------------------------------------------------------------------------

        internal void UpdateEdgeIntoAEL(ref TEdge e)
        {
            if (e.NextInLML.IsNull)
                throw new ClipperException("UpdateEdgeIntoAEL: invalid call");
            TEdge AelPrev = e.PrevInAEL;
            TEdge AelNext = e.NextInAEL;
            e.NextInLML.OutIdx = e.OutIdx;
            if (AelPrev.NotNull)
                AelPrev.NextInAEL = e.NextInLML;
            else m_ActiveEdges = e.NextInLML;
            if (AelNext.NotNull)
                AelNext.PrevInAEL = e.NextInLML;
            e.NextInLML.Side = e.Side;
            e.NextInLML.WindDelta = e.WindDelta;
            e.NextInLML.WindCnt = e.WindCnt;
            e.NextInLML.WindCnt2 = e.WindCnt2;
            e = e.NextInLML;
            e.Curr = e.Bot;
            e.PrevInAEL = AelPrev;
            e.NextInAEL = AelNext;
            if (!IsHorizontal(ref e)) InsertScanbeam(ref e.Top.Y);
        }

        //------------------------------------------------------------------------------

        internal void SwapPositionsInAEL(ref TEdge edge1, ref TEdge edge2)
        {
            //check that one or other edge hasn't already been removed from AEL ...
            if (edge1.NextInAEL == edge1.PrevInAEL ||
                edge2.NextInAEL == edge2.PrevInAEL) return;

            if (edge1.NextInAEL == edge2)
            {
                TEdge next = edge2.NextInAEL;
                if (next.NotNull)
                    next.PrevInAEL = edge1;
                TEdge prev = edge1.PrevInAEL;
                if (prev.NotNull)
                    prev.NextInAEL = edge2;
                edge2.PrevInAEL = prev;
                edge2.NextInAEL = edge1;
                edge1.PrevInAEL = edge2;
                edge1.NextInAEL = next;
            }
            else if (edge2.NextInAEL == edge1)
            {
                TEdge next = edge1.NextInAEL;
                if (next.NotNull)
                    next.PrevInAEL = edge2;
                TEdge prev = edge2.PrevInAEL;
                if (prev.NotNull)
                    prev.NextInAEL = edge1;
                edge1.PrevInAEL = prev;
                edge1.NextInAEL = edge2;
                edge2.PrevInAEL = edge1;
                edge2.NextInAEL = next;
            }
            else
            {
                TEdge next = edge1.NextInAEL;
                TEdge prev = edge1.PrevInAEL;
                edge1.NextInAEL = edge2.NextInAEL;
                if (edge1.NextInAEL.NotNull)
                    edge1.NextInAEL.PrevInAEL = edge1;
                edge1.PrevInAEL = edge2.PrevInAEL;
                if (edge1.PrevInAEL.NotNull)
                    edge1.PrevInAEL.NextInAEL = edge1;
                edge2.NextInAEL = next;
                if (edge2.NextInAEL.NotNull)
                    edge2.NextInAEL.PrevInAEL = edge2;
                edge2.PrevInAEL = prev;
                if (edge2.PrevInAEL.NotNull)
                    edge2.PrevInAEL.NextInAEL = edge2;
            }

            if (edge1.PrevInAEL.IsNull)
                m_ActiveEdges = edge1;
            else if (edge2.PrevInAEL.IsNull)
                m_ActiveEdges = edge2;
        }

        //------------------------------------------------------------------------------

        internal void DeleteFromAEL(ref TEdge e)
        {
            TEdge AelPrev = e.PrevInAEL;
            TEdge AelNext = e.NextInAEL;
            if (AelPrev.IsNull && AelNext.IsNull && (e != m_ActiveEdges))
                return; //already deleted
            if (AelPrev.NotNull)
                AelPrev.NextInAEL = AelNext;
            else m_ActiveEdges = AelNext;
            if (AelNext.NotNull)
                AelNext.PrevInAEL = AelPrev;
            e.NextInAEL.SetNull();
            e.PrevInAEL.SetNull();
        }
    }
}
