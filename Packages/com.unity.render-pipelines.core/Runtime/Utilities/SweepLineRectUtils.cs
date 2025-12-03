using System.Buffers;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class that computes the total rect area via a sweep-line.
    /// </summary>
    public static class SweepLineRectUtils
    {
        struct EventComparer : IComparer<Vector4>
        {
            public int Compare(Vector4 a, Vector4 b)
            {
                int cx = a.x.CompareTo(b.x);
                if (cx != 0) return cx;
                // tie on x: larger y first (+1 before -1)
                return b.y.CompareTo(a.y);
            }
        }
        struct ActiveComparer : IComparer<Vector2>
        {
            public int Compare(Vector2 a, Vector2 b)
            {
                return a.x.CompareTo(b.x);
            }
        }

        /// <summary>
        /// Computes the total covered area (union) of a set of axis-aligned rectangles, counting overlaps only once.
        /// </summary>
        /// <param name="rects">List of rects to compute.</param>
        /// <returns>The normalized union area in [0,1], with overlaps counted once.</returns>
        public static float CalculateRectUnionArea(List<Rect> rects)
        {
            int rectsCount = rects.Count;
            var eventsBuffer = ArrayPool<Vector4>.Shared.Rent(rectsCount * 2);
            var activeBuffer = ArrayPool<Vector2>.Shared.Rent(rectsCount);

            int eventCount = 0;
            foreach (var rect in rects)
                InsertEvents(rect, eventsBuffer, ref eventCount);

            float area = CalculateRectUnionArea(eventsBuffer, activeBuffer, eventCount);
            ArrayPool<Vector4>.Shared.Return(eventsBuffer);
            ArrayPool<Vector2>.Shared.Return(activeBuffer);

            return area;
        }

        // Merge overlapping intervals and return total covered Y length
        static float MergeLengthY(Vector2[] activeBuffer, int count)
        {
            if (count <= 0)
                return 0f;

            // ActiveBuffer stores (yMin, yMax)
            float total = 0f;
            float cy0 = activeBuffer[0].x;
            float cy1 = activeBuffer[0].y;

            for (int i = 1; i < count; i++)
            {
                float y0 = activeBuffer[i].x;
                float y1 = activeBuffer[i].y;
                if (y0 <= cy1)
                {
                    if (y1 > cy1) cy1 = y1;
                }
                else
                {
                    total += (cy1 - cy0);
                    cy0 = y0; cy1 = y1;
                }
            }
            total += (cy1 - cy0);
            return Mathf.Clamp01(total);
        }

        /// <summary>
        /// Computes the total covered area (union) of a set of axis-aligned rectangles using a sweep-line.
        /// </summary>
        static unsafe float CalculateRectUnionArea(Vector4[] eventsBuffer, Vector2[] activeBuffer, int eventCount)
        {
            if (eventCount == 0)
                return 0f;

            // Sort events by (x asc, enter first)
            fixed (Vector4* ptr = eventsBuffer)
            {
                NativeSortExtension.Sort(ptr, eventCount, new EventComparer());
            }

            int activeCount = 0;
            float area = 0f;
            float lastX = eventsBuffer[0].x;
            bool needLastXUpdate = false;
            int i = 0;

            while (i < eventCount)
            {
                float x = eventsBuffer[i].x;

                if (needLastXUpdate)
                {
                    lastX = x;
                    needLastXUpdate = false;
                }

                // Accumulate area over [lastX, x)
                float dx = x - lastX;
                if (dx > 0f && activeCount > 0)
                {
                    fixed (Vector2* ptr = activeBuffer)
                    {
                        NativeSortExtension.Sort(ptr, activeCount, new ActiveComparer());
                    }
                    area += MergeLengthY(activeBuffer, activeCount) * dx;
                    lastX = x;
                }

                // Consume all events at this x (group approx-equal x's)
                do
                {
                    Vector4 ev = eventsBuffer[i];
                    float y0 = ev.z;
                    float y1 = ev.w;

                    if (ev.y > 0f) // Enter
                    {
                        activeBuffer[activeCount++] = new Vector2(y0, y1);
                    }
                    else // Leave
                    {
                        // Remove once (swap with last)
                        for (int k = 0; k < activeCount; k++)
                        {
                            Vector2 v = activeBuffer[k];
                            if (Mathf.Approximately(v.x, y0) && Mathf.Approximately(v.y, y1))
                            {
                                int last = activeCount - 1;
                                activeBuffer[k] = activeBuffer[last];
                                activeCount = last;
                                break;
                            }
                        }

                        if (activeCount == 0)
                            needLastXUpdate = true;
                    }

                    i++;
                }
                while (i < eventCount && Mathf.Approximately(eventsBuffer[i].x, x));
            }

            return area;
        }

        // Insert events with clamped rects
        static void InsertEvents(in Rect rect, Vector4[] eventsBuffer, ref int eventCount)
        {
            if (rect.width > 0f && rect.height > 0f)
            {
                var y0 = Mathf.Clamp01(rect.yMin);
                var y1 = Mathf.Clamp01(rect.yMax);
                eventsBuffer[eventCount++] = new Vector4(Mathf.Clamp01(rect.xMin), +1f, y0, y1); // enter
                eventsBuffer[eventCount++] = new Vector4(Mathf.Clamp01(rect.xMax), -1f, y0, y1); // leave
            }
        }
    }
}
