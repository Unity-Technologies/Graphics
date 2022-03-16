using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToSpacingStrategy : SnapStrategy
    {
        class SnapToSpacingResult : SnapResult
        {
            public ReferenceRects ReferenceRects;
        }

        struct ReferenceRects
        {
            public List<Rect> Rects;
            public PortOrientation Orientation;
        }

        class SpacingLine
        {
            public const float DefaultSpacingLineSideLength = 10f;
            public Line StartSideLine;
            public Line LineInBetween;
            public Line EndSideLine;

            public List<Line> Lines => new List<Line> { StartSideLine, EndSideLine, LineInBetween };
        }

        static Vector2 GetMaxPos(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return new Vector2(rect.x, rect.yMax);
                case SnapReference.HorizontalCenter:
                    return new Vector2(rect.center.x, rect.yMax);
                case SnapReference.RightEdge:
                    return new Vector2(rect.xMax, rect.yMax);
                case SnapReference.TopEdge:
                    return new Vector2(rect.xMax, rect.y);
                case SnapReference.VerticalCenter:
                    return new Vector2(rect.xMax, rect.center.y);
                case SnapReference.BottomEdge:
                    return new Vector2(rect.xMax, rect.yMax);
                default:
                    return Vector2.zero;
            }
        }

        LineView m_LineView;
        Dictionary<float, ReferenceRects> m_SpacingPositions = new Dictionary<float, ReferenceRects>();

        ReferenceRects m_VerticalReferenceRects = new ReferenceRects
        {
            Rects = new List<Rect>(),
            Orientation = PortOrientation.Vertical
        };

        ReferenceRects m_HorizontalReferenceRects = new ReferenceRects
        {
            Rects = new List<Rect>(),
            Orientation = PortOrientation.Horizontal
        };

        public override void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to spacing already active. Call EndSnap() first.");
            }
            IsActive = true;

            m_GraphView = selectedElement.GraphView;
            if (m_LineView == null)
            {
                m_LineView = new LineView(m_GraphView);
            }
            m_GraphView.Add(m_LineView);
        }

        public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.GetSnappedRect: Snap to spacing not active. Call BeginSnap() first.");
            }
            if (IsPaused)
            {
                // Snapping was paused, we do not return a snapped rect and we clear the snap lines
                ClearSnapLines();
                return sourceRect;
            }

            Rect selectedElementRect = (selectedElement).parent.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, (selectedElement).layout);
            UpdateSpacingPositions(selectedElement, selectedElementRect);

            Rect snappedRect = sourceRect;
            m_CurrentScale = scale;

            m_LineView.lines.Clear();

            List<SnapToSpacingResult> results = GetClosestSpacingPositions(sourceRect);

            foreach (var result in results.Where(result => result != null))
            {
                ApplySnapToSpacingResult(ref snappingOffset, sourceRect, ref snappedRect, result);

                // Make sure the element is snapped before drawing the lines
                if (IsSnapped(snappedRect, sourceRect, result.ReferenceRects.Orientation))
                {
                    foreach (SpacingLine spacingLine in GetSpacingLines(result.ReferenceRects.Rects, result.ReferenceRects.Orientation))
                    {
                        m_LineView.lines.AddRange(spacingLine.Lines);
                    }
                }
            }
            m_LineView.MarkDirtyRepaint();

            return snappedRect;
        }

        public override void EndSnap()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.EndSnap: Snap to spacing already inactive. Call BeginSnap() first.");
            }
            IsActive = false;

            ClearRectsToConsider();

            m_LineView.lines.Clear();
            m_LineView.Clear();
            m_LineView.RemoveFromHierarchy();
        }

        bool IsSnapped(Rect snappedRect, Rect sourceRect, PortOrientation orientation)
        {
            float draggedDistance = Math.Abs(orientation == PortOrientation.Horizontal ? snappedRect.x - sourceRect.x : snappedRect.y - sourceRect.y);

            return draggedDistance < (SnapDistance - 1) / m_CurrentScale;
        }

        void ClearRectsToConsider()
        {
            m_HorizontalReferenceRects.Rects.Clear();
            m_VerticalReferenceRects.Rects.Clear();
            m_SpacingPositions.Clear();
        }

        void UpdateSpacingPositions(GraphElement selectedElement, Rect sourceRect)
        {
            ClearRectsToConsider();
            GetRectsToConsiderInView(selectedElement);
            SortReferenceRects();
            ComputeSpacingPositions(m_VerticalReferenceRects, sourceRect);
            ComputeSpacingPositions(m_HorizontalReferenceRects, sourceRect);
        }

        static readonly List<ModelView> k_GetRectsToConsiderInViewAllUIs = new List<ModelView>();
        void GetRectsToConsiderInView(GraphElement selectedElement)
        {
            // Consider only the visible nodes.
            Rect rectToFit = m_GraphView.layout;

            m_GraphView.GraphModel.GraphElementModels.GetAllViewsInList(m_GraphView, null, k_GetRectsToConsiderInViewAllUIs);
            foreach (GraphElement element in k_GetRectsToConsiderInViewAllUIs.OfType<GraphElement>())
            {
                if (!IsIgnoredElement(selectedElement, element, rectToFit))
                {
                    Rect geometryInContentViewContainerSpace = element.parent.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, element.layout);
                    AddReferenceRects(selectedElement, element, geometryInContentViewContainerSpace);
                }
            }

            k_GetRectsToConsiderInViewAllUIs.Clear();
        }

        bool IsIgnoredElement(GraphElement selectedElement, GraphElement element, Rect rectToFit)
        {
            if (selectedElement is Placemat placemat && element.layout.Overlaps(placemat.layout) || element is Edge || !element.visible
                || element.IsSelected() || element.layout.Overlaps(selectedElement.layout))
            {
                return true;
            }

            Rect localSelRect = m_GraphView.ChangeCoordinatesTo(element, rectToFit);
            return !element.Overlaps(localSelRect);
        }

        void AddReferenceRects(GraphElement selectedElement, GraphElement element, Rect rectToAdd)
        {
            // Check if element is within selectedElement's vertical boundaries before adding
            if (AreElementsSuperposed(selectedElement.layout, element.layout, PortOrientation.Horizontal))
            {
                m_VerticalReferenceRects.Rects.Add(rectToAdd);
            }
            // Check if element is within selectedElement's horizontal boundaries before adding
            if (AreElementsSuperposed(selectedElement.layout, element.layout, PortOrientation.Vertical))
            {
                m_HorizontalReferenceRects.Rects.Add(rectToAdd);
            }
        }

        void SortReferenceRects()
        {
            // We want to iterate through rects from the start to the end (xMin to xMax OR yMin to yMax depending on the orientation)
            m_VerticalReferenceRects.Rects.Sort((rectA, rectB) => rectA.yMax.CompareTo(rectB.yMax));
            m_HorizontalReferenceRects.Rects.Sort((rectA, rectB) => rectA.xMax.CompareTo(rectB.xMax));
        }

        void ComputeSpacingPositions(ReferenceRects referenceRects, Rect sourceRect)
        {
            SnapReference startReference = referenceRects.Orientation == PortOrientation.Vertical ? SnapReference.TopEdge : SnapReference.LeftEdge;
            SnapReference endReference = referenceRects.Orientation == PortOrientation.Vertical ? SnapReference.BottomEdge : SnapReference.RightEdge;

            for (int i = 0; i < referenceRects.Rects.Count; ++i)
            {
                Rect firstRect = referenceRects.Rects[i];
                int nextRectIndex = i + 1; // After rect i is done, we don't consider it anymore for the next iterations

                for (int j = nextRectIndex; j < referenceRects.Rects.Count; ++j)
                {
                    Rect secondRect = referenceRects.Rects[j];

                    // For each rect i, we find the 3 spacing positions: (examples are for horizontal orientation)
                    //        - 1. position before rect i
                    //              +-----+    +-----+    +-----+
                    //              | pos |    |  i  |    |  j  |
                    //              +-----+    +-----+    +-----+
                    //        - 2. position between rect i and rect j
                    //              +-----+    +-----+    +-----+
                    //              |  i  |    | pos |    |  j  |
                    //              +-----+    +-----+    +-----+
                    //        - 3. position after rect j
                    //              +-----+    +-----+    +-----+
                    //              |  i  |    |  j  |    | pos |
                    //              +-----+    +-----+    +-----+

                    List<float> spacingPositions = GetSpacingPositions(sourceRect, firstRect, secondRect, startReference, endReference, referenceRects.Orientation);
                    AddSpacingPositions(spacingPositions, sourceRect, firstRect, secondRect, referenceRects.Orientation);
                }
            }
        }

        static bool AreElementsSuperposed(Rect firstElementRect, Rect secondElementRect, PortOrientation orientation)
        {
            if (orientation == PortOrientation.Vertical)
            {
                if (firstElementRect.yMin < secondElementRect.yMax && firstElementRect.yMax > secondElementRect.yMin)
                {
                    return true;
                }
            }
            else
            {
                if (firstElementRect.xMin < secondElementRect.xMax && firstElementRect.xMax > secondElementRect.xMin)
                {
                    return true;
                }
            }

            return false;
        }

        static List<float> GetSpacingPositions(Rect sourceRect, Rect firstRect, Rect secondRect, SnapReference startReference, SnapReference endReference, PortOrientation orientation)
        {
            if (AreElementsSuperposed(firstRect, secondRect, orientation))
            {
                return null;
            }

            Vector2 firstRectStartPos = GetMaxPos(firstRect, startReference);
            Vector2 firstRectEndPos = GetMaxPos(firstRect, endReference);
            Vector2 secondRectStartPos = GetMaxPos(secondRect, startReference);
            Vector2 secondRectEndPos = GetMaxPos(secondRect, endReference);

            List<float> positions = orientation == PortOrientation.Vertical ?
                ComputeSpacingPositions(firstRectStartPos.y, firstRectEndPos.y, secondRectStartPos.y, secondRectEndPos.y, sourceRect.height * 0.5f) :
                ComputeSpacingPositions(firstRectStartPos.x, firstRectEndPos.x, secondRectStartPos.x, secondRectEndPos.x, sourceRect.width * 0.5f);

            return positions;
        }

        static List<float> ComputeSpacingPositions(float firstRectStartPos, float firstRectEndPos, float secondRectStartPos, float secondRectEndPos, float sourceRectOffset)
        {
            List<float> positions = new List<float>(3); // There are always 3 positions

            float distance = Math.Abs(firstRectEndPos - secondRectStartPos);
            float offset = distance + sourceRectOffset;

            // Position before firstRect
            positions.Add(firstRectStartPos - offset);

            // Position between firstRect and secondRect
            positions.Add(firstRectEndPos + distance * 0.5f);

            // Position after secondRect
            positions.Add(secondRectEndPos + offset);

            return positions;
        }

        void AddSpacingPositions(List<float> positions, Rect sourceRect, Rect firstRect, Rect secondRect, PortOrientation orientation)
        {
            if (positions != null)
            {
                // Position before firstRect
                AddSpacingPosition(positions[0], new ReferenceRects
                {
                    Rects = new List<Rect> { sourceRect, firstRect, secondRect },
                    Orientation = orientation
                });

                // Position between firstRect and secondRect
                AddSpacingPosition(positions[1], new ReferenceRects
                {
                    Rects = new List<Rect> { firstRect, sourceRect, secondRect },
                    Orientation = orientation
                });

                // Position after secondRect
                AddSpacingPosition(positions[2], new ReferenceRects
                {
                    Rects = new List<Rect> { firstRect, secondRect, sourceRect },
                    Orientation = orientation
                });
            }
        }

        void AddSpacingPosition(float spacingPos, ReferenceRects referenceRects)
        {
            if (!m_SpacingPositions.ContainsKey(spacingPos))
            {
                m_SpacingPositions.Add(spacingPos, referenceRects);
            }
        }

        SnapToSpacingResult GetClosestSpacingPosition(Rect sourceRect, PortOrientation orientation)
        {
            SnapToSpacingResult minResult = null;
            float minDistance = float.MaxValue;

            foreach (var spacingPos in m_SpacingPositions.Where(spacingPos => spacingPos.Value.Orientation == orientation))
            {
                SnapToSpacingResult result = GetSnapToSpacingResult(sourceRect, spacingPos.Key, spacingPos.Value);
                if (result != null && minDistance > result.Distance)
                {
                    minDistance = result.Distance;
                    minResult = result;
                }
            }

            return minResult;
        }

        List<SnapToSpacingResult> GetClosestSpacingPositions(Rect sourceRect)
        {
            List<SnapToSpacingResult> results = new List<SnapToSpacingResult>();

            SnapToSpacingResult horizontalResult = GetClosestSpacingPosition(sourceRect, PortOrientation.Horizontal);
            if (horizontalResult != null)
            {
                results.Add(horizontalResult);
            }

            SnapToSpacingResult verticalResult = GetClosestSpacingPosition(sourceRect, PortOrientation.Vertical);
            if (verticalResult != null)
            {
                results.Add(verticalResult);
            }

            return results;
        }

        SnapToSpacingResult GetSnapToSpacingResult(Rect sourceRect, float middlePos, ReferenceRects referenceRects)
        {
            float sourceRectCenter = referenceRects.Orientation == PortOrientation.Vertical ? GetMaxPos(sourceRect, SnapReference.VerticalCenter).y : GetMaxPos(sourceRect, SnapReference.HorizontalCenter).x;
            float offset = sourceRectCenter - middlePos;

            SnapToSpacingResult minResult = new SnapToSpacingResult
            {
                Offset = offset,
                ReferenceRects = referenceRects
            };

            return minResult.Distance <= SnapDistance * 1 / m_CurrentScale ? minResult : null;
        }

        static SpacingLine GetSpacingLine(float maxCoordinate, float spacingLineSideLength, Vector2 startPos, Vector2 endPos, PortOrientation orientation)
        {
            // Start side's line of spacingLine
            Vector2 start = orientation == PortOrientation.Vertical ? new Vector2(maxCoordinate, startPos.y) : new Vector2(startPos.x, maxCoordinate);
            Vector2 end = startPos;
            Line startSideLine = new Line(start, end);

            // Line in between of spacingLine
            float linePos = maxCoordinate - spacingLineSideLength * 0.5f;

            start = orientation == PortOrientation.Vertical ? new Vector2(linePos, startPos.y) : new Vector2(startPos.x, linePos);
            end = orientation == PortOrientation.Vertical ? new Vector2(linePos, endPos.y) : new Vector2(endPos.x, linePos);
            Line lineInBetween = new Line(start, end);

            // End side's line of spacingLine
            start = orientation == PortOrientation.Vertical ? new Vector2(maxCoordinate, endPos.y) : new Vector2(endPos.x, maxCoordinate);
            end = endPos;
            Line endSideLine = new Line(start, end);

            return new SpacingLine
            {
                StartSideLine = startSideLine,
                LineInBetween = lineInBetween,
                EndSideLine = endSideLine
            };
        }

        List<SpacingLine> GetSpacingLines(List<Rect> rects, PortOrientation orientation)
        {
            SnapReference startReference = orientation == PortOrientation.Vertical ? SnapReference.BottomEdge : SnapReference.RightEdge;
            SnapReference endReference = orientation == PortOrientation.Vertical ? SnapReference.TopEdge : SnapReference.LeftEdge;

            float maxCoordinate = rects.Max(rect => orientation == PortOrientation.Vertical ? rect.xMax : rect.yMax) + SpacingLine.DefaultSpacingLineSideLength;
            float spacingLineSideLength = SpacingLine.DefaultSpacingLineSideLength / m_CurrentScale;

            Vector2 firstSidePos = GetMaxPos(rects[0], startReference);
            Vector2 secondSidePos = GetMaxPos(rects[1], endReference);
            Vector2 thirdSidePos = GetMaxPos(rects[1], startReference);
            Vector2 fourthSidePos = GetMaxPos(rects[2], endReference);

            return new List<SpacingLine>
            {
                GetSpacingLine(maxCoordinate, spacingLineSideLength, firstSidePos, secondSidePos, orientation),
                GetSpacingLine(maxCoordinate, spacingLineSideLength, thirdSidePos, fourthSidePos, orientation)
            };
        }

        static void ApplySnapToSpacingResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToSpacingResult result)
        {
            if (result.ReferenceRects.Orientation == PortOrientation.Horizontal)
            {
                r1.x = sourceRect.x - result.Offset;
                snappingOffset.x = snappingOffset.x < float.MaxValue ? snappingOffset.x + result.Offset : result.Offset;
            }
            else
            {
                r1.y = sourceRect.y - result.Offset;
                snappingOffset.y = snappingOffset.y < float.MaxValue ? snappingOffset.y + result.Offset : result.Offset;
            }
        }

        void ClearSnapLines()
        {
            m_LineView.lines.Clear();
            m_LineView.MarkDirtyRepaint();
        }
    }
}
