using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToGridStrategy : SnapStrategy
    {
        class SnapToGridResult : SnapResult
        {
            public SnapReference SnappableReference { get; set; }
        }

        internal struct BorderWidth
        {
            public float Top { get; set; }
            public float Bottom { get; set; }
            public float Left { get; set; }
            public float Right { get; set; }
        }

        float m_GridSpacing;
        BorderWidth m_BorderWidth;

        public override void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                Debug.LogError("SnapService.BeginSnap: Snap to grid already active. Call EndSnap() first.");
            }
            IsActive = true;

            m_BorderWidth = GetBorderWidth(selectedElement); // Needed to snap element on its content container's border

            m_GraphView = selectedElement.GraphView;

            m_GridSpacing = m_GraphView.SafeQ<GridBackground>().Spacing;
        }

        public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement)
        {
            if (!IsActive)
            {
                Debug.LogError("SnapService.GetSnappedRect: Snap to grid not active. Call BeginSnap() first.");
            }

            if (IsPaused)
            {
                // Snapping was paused, we do not return a snapped rect
                return sourceRect;
            }

            Rect snappedRect = sourceRect;

            List<SnapToGridResult> results = GetClosestGridLines(sourceRect);

            foreach (SnapToGridResult result in results)
            {
                ApplySnapToGridResult(ref snappingOffset, sourceRect, ref snappedRect, result);
            }

            return snappedRect;
        }

        public override void EndSnap()
        {
            if (!IsActive)
            {
                Debug.LogError("SnapService.EndSnap: Snap to grid already inactive. Call BeginSnap() first.");
            }
            IsActive = false;
        }

        SnapToGridResult GetClosestGridLine(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference endReference)
        {
            float sourcePos = GetPositionWithBorder(sourceRect, sourceRef);
            float offsetStart = sourcePos - GetClosestGridLine(sourceRect, startReference);
            float offsetEnd = sourcePos - GetClosestGridLine(sourceRect, endReference);
            float minOffset = offsetStart;

            SnapReference minSnappableReference = startReference;

            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }

            SnapToGridResult minResult = new SnapToGridResult()
            {
                SnappableReference = minSnappableReference,
                Offset = minOffset
            };

            return minResult.Distance <= SnapDistance ? minResult : null;
        }

        List<SnapToGridResult> GetClosestGridLines(Rect sourceRect, PortOrientation orientation)
        {
            SnapReference startReference = orientation == PortOrientation.Horizontal ? SnapReference.LeftEdge : SnapReference.TopEdge;
            SnapReference centerReference = orientation == PortOrientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            SnapReference endReference = orientation == PortOrientation.Horizontal ? SnapReference.RightEdge : SnapReference.BottomEdge;

            List<SnapToGridResult> results = new List<SnapToGridResult>(3);
            SnapToGridResult result = GetClosestGridLine(sourceRect, startReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestGridLine(sourceRect, centerReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestGridLine(sourceRect, endReference, startReference, endReference);
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

        List<SnapToGridResult> GetClosestGridLines(Rect sourceRect)
        {
            List<SnapToGridResult> results = GetClosestGridLines(sourceRect, PortOrientation.Horizontal);
            return results.Union(GetClosestGridLines(sourceRect, PortOrientation.Vertical)).ToList();
        }

        static void ApplySnapToGridResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToGridResult result)
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

        internal static BorderWidth GetBorderWidth(GraphElement element)
        {
            var borderWidth = new BorderWidth
            {
                Top = element.contentContainer.resolvedStyle.borderTopWidth,
                Bottom = element.contentContainer.resolvedStyle.borderBottomWidth,
                Left = element.contentContainer.resolvedStyle.borderLeftWidth,
                Right = element.contentContainer.resolvedStyle.borderRightWidth
            };

            return borderWidth;
        }

        float GetPositionWithBorder(Rect rect, SnapReference reference)
        {
            // We need to take account of the selected element's content container's border width to snap on it
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return rect.x - m_BorderWidth.Left;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightEdge:
                    return rect.xMax + m_BorderWidth.Right;
                case SnapReference.TopEdge:
                    return rect.y - m_BorderWidth.Top;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomEdge:
                    return rect.yMax + m_BorderWidth.Bottom;
                default:
                    return 0;
            }
        }

        float GetClosestGridLine(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return GetClosestGridLine(rect.xMin);
                case SnapReference.HorizontalCenter:
                    return GetClosestGridLine(rect.center.x);
                case SnapReference.RightEdge:
                    return GetClosestGridLine(rect.xMax);
                case SnapReference.TopEdge:
                    return GetClosestGridLine(rect.yMin);
                case SnapReference.VerticalCenter:
                    return GetClosestGridLine(rect.center.y);
                case SnapReference.BottomEdge:
                    return GetClosestGridLine(rect.yMax);
                default:
                    return 0;
            }
        }

        float GetClosestGridLine(float elementPosition)
        {
            // To find the closest grid line, we count the number of grid spacing to the selected element and we round it to the nearest grid line
            int spacingCount = (int)Math.Round(elementPosition / m_GridSpacing, 0);

            return spacingCount * m_GridSpacing;
        }
    }
}
