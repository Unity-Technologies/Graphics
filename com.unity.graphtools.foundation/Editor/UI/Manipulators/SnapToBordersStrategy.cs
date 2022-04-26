using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToBordersStrategy : SnapStrategy
    {
        class SnapToBordersResult : SnapResult
        {
            public SnapReference SourceReference { get; set; }
            public SnapReference SnappableReference { get; set; }
            public Line IndicatorLine;
        }

        static float GetPos(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return rect.x;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightEdge:
                    return rect.xMax;
                case SnapReference.TopEdge:
                    return rect.y;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomEdge:
                    return rect.yMax;
                default:
                    return 0;
            }
        }

        LineView m_LineView;
        List<Rect> m_SnappableRects = new List<Rect>();

        public override void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to borders already active. Call EndSnap() first.");
            }
            IsActive = true;

            m_GraphView = selectedElement.GraphView;
            if (m_LineView == null)
            {
                m_LineView = new LineView(m_GraphView);
            }

            m_GraphView.Add(m_LineView);
            m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);
        }

        public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.GetSnappedRect: Snap to borders not active. Call BeginSnap() first.");
            }

            if (IsPaused)
            {
                // Snapping was paused, we do not return a snapped rect and we clear the snap lines
                ClearSnapLines();
                return sourceRect;
            }

            Rect snappedRect = sourceRect;

            List<SnapToBordersResult> results = GetClosestSnapElements(sourceRect);

            m_LineView.lines.Clear();

            foreach (SnapToBordersResult result in results)
            {
                ApplySnapToBordersResult(ref snappingOffset, sourceRect, ref snappedRect, result);
                result.IndicatorLine = GetSnapLine(snappedRect, result.SourceReference, result.SnappableRect, result.SnappableReference);
                m_LineView.lines.Add(result.IndicatorLine);
            }
            m_LineView.MarkDirtyRepaint();

            m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);

            return snappedRect;
        }

        public override void EndSnap()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.EndSnap: Snap to borders already inactive. Call BeginSnap() first.");
            }
            IsActive = false;

            m_SnappableRects.Clear();
            m_LineView.lines.Clear();
            m_LineView.Clear();
            m_LineView.RemoveFromHierarchy();
        }

        static readonly List<ModelView> k_GetNotSelectedElementRectsInViewAllUIs = new List<ModelView>();
        List<Rect> GetNotSelectedElementRectsInView(GraphElement selectedElement)
        {
            var notSelectedElementRects = new List<Rect>();
            var ignoredModels = m_GraphView.GetSelection().Cast<IModel>().ToList();

            // Consider only the visible nodes.
            Rect rectToFit = m_GraphView.layout;

            m_GraphView.GraphModel.GraphElementModels.GetAllViewsInList(m_GraphView, null, k_GetNotSelectedElementRectsInViewAllUIs);
            foreach (ModelView element in k_GetNotSelectedElementRectsInViewAllUIs)
            {
                if (selectedElement is Placemat placemat && element.layout.Overlaps(placemat.layout))
                {
                    // If the selected element is a placemat, we do not consider the elements under it
                    ignoredModels.Add(element.Model);
                }
                else if (element is Edge)
                {
                    // Don't consider edges
                    ignoredModels.Add(element.Model);
                }
                else if (!element.visible)
                {
                    // Don't consider not visible elements
                    ignoredModels.Add(element.Model);
                }
                else if (element is GraphElement ge && !ge.IsSelected() && !(ignoredModels.Contains(element.Model)))
                {
                    var localSelRect = m_GraphView.ChangeCoordinatesTo(element, rectToFit);
                    if (element.Overlaps(localSelRect))
                    {
                        Rect geometryInContentViewContainerSpace = (element).parent.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, ge.layout);
                        notSelectedElementRects.Add(geometryInContentViewContainerSpace);
                    }
                }
            }

            k_GetNotSelectedElementRectsInViewAllUIs.Clear();

            return notSelectedElementRects;
        }

        SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, Rect snappableRect, SnapReference startReference, SnapReference endReference)
        {
            float sourcePos = GetPos(sourceRect, sourceRef);
            float offsetStart = sourcePos - GetPos(snappableRect, startReference);
            float offsetEnd = sourcePos - GetPos(snappableRect, endReference);
            float minOffset = offsetStart;
            SnapReference minSnappableReference = startReference;
            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }
            SnapToBordersResult minResult = new SnapToBordersResult
            {
                SourceReference = sourceRef,
                SnappableRect = snappableRect,
                SnappableReference = minSnappableReference,
                Offset = minOffset
            };

            return minResult.Distance <= SnapDistance ? minResult : null;
        }

        SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference endReference)
        {
            SnapToBordersResult minResult = null;
            float minDistance = float.MaxValue;
            foreach (Rect snappableRect in m_SnappableRects)
            {
                SnapToBordersResult result = GetClosestSnapElement(sourceRect, sourceRef, snappableRect, startReference, endReference);
                if (result != null && minDistance > result.Distance)
                {
                    minDistance = result.Distance;
                    minResult = result;
                }
            }
            return minResult;
        }

        List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect, PortOrientation orientation)
        {
            SnapReference startReference = orientation == PortOrientation.Horizontal ? SnapReference.LeftEdge : SnapReference.TopEdge;
            SnapReference centerReference = orientation == PortOrientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            SnapReference endReference = orientation == PortOrientation.Horizontal ? SnapReference.RightEdge : SnapReference.BottomEdge;
            List<SnapToBordersResult> results = new List<SnapToBordersResult>(3);
            SnapToBordersResult result = GetClosestSnapElement(sourceRect, startReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, centerReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, endReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            // Look for the minimum
            if (results.Count > 0)
            {
                results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                float minDistance = results[0].Distance;
                results.RemoveAll(r => Math.Abs(r.Distance - minDistance) > 0.01f);
            }
            return results;
        }

        List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect)
        {
            List<SnapToBordersResult> snapToBordersResults = GetClosestSnapElements(sourceRect, PortOrientation.Horizontal);
            return snapToBordersResults.Union(GetClosestSnapElements(sourceRect, PortOrientation.Vertical)).ToList();
        }

        static Line GetSnapLine(Rect r, SnapReference reference)
        {
            Vector2 start;
            Vector2 end;
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    start = r.position;
                    end = new Vector2(r.x, r.yMax);
                    break;
                case SnapReference.HorizontalCenter:
                    start = r.center;
                    end = start;
                    break;
                case SnapReference.RightEdge:
                    start = new Vector2(r.xMax, r.yMin);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
                case SnapReference.TopEdge:
                    start = r.position;
                    end = new Vector2(r.xMax, r.yMin);
                    break;
                case SnapReference.VerticalCenter:
                    start = r.center;
                    end = start;
                    break;
                default: // case SnapReference.BottomEdge:
                    start = new Vector2(r.x, r.yMax);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
            }
            return new Line(start, end);
        }

        static Line GetSnapLine(Rect r1, SnapReference reference1, Rect r2, SnapReference reference2)
        {
            bool horizontal = reference1 <= SnapReference.RightEdge;
            Line line1 = GetSnapLine(r1, reference1);
            Line line2 = GetSnapLine(r2, reference2);
            Vector2 p11 = line1.Start;
            Vector2 p12 = line1.End;
            Vector2 p21 = line2.Start;
            Vector2 p22 = line2.End;
            Vector2 start;
            Vector2 end;

            if (horizontal)
            {
                float x = p21.x;
                float yMin = Math.Min(p22.y, Math.Min(p21.y, Math.Min(p11.y, p12.y)));
                float yMax = Math.Max(p22.y, Math.Max(p21.y, Math.Max(p11.y, p12.y)));
                start = new Vector2(x, yMin);
                end = new Vector2(x, yMax);
            }
            else
            {
                float y = p22.y;
                float xMin = Math.Min(p22.x, Math.Min(p21.x, Math.Min(p11.x, p12.x)));
                float xMax = Math.Max(p22.x, Math.Max(p21.x, Math.Max(p11.x, p12.x)));
                start = new Vector2(xMin, y);
                end = new Vector2(xMax, y);
            }
            return new Line(start, end);
        }

        static void ApplySnapToBordersResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToBordersResult result)
        {
            if (result.SnappableReference <= SnapReference.RightEdge)
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
