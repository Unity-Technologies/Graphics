using System;
using UnityEngine;

namespace UnityEditorInternal.Experimental
{
    internal class RectUtils
    {
        public static bool Contains(Rect a, Rect b)
        {
            if (a.xMin > b.xMin)
                return false;
            if (a.xMax < b.xMax)
                return false;

            if (a.yMin > b.yMin)
                return false;
            if (a.yMax < b.yMax)
                return false;

            return true;
        }

        public static Rect Encompass(Rect a, Rect b)
        {
            Rect newRect = a;
            newRect.xMin = Math.Min(a.xMin, b.xMin);
            newRect.yMin = Math.Min(a.yMin, b.yMin);
            newRect.xMax = Math.Max(a.xMax, b.xMax);
            newRect.yMax = Math.Max(a.yMax, b.yMax);
            return newRect;
        }

        public static Rect Inflate(Rect a, float factor)
        {
            return Inflate(a, factor, factor);
        }

        public static Rect Inflate(Rect a, float factorX, float factorY)
        {
            float newWidth = a.width * factorX;
            float newHeight = a.height * factorY;

            float offsetWidth = (newWidth - a.width) / 2.0f;
            float offsetHeight = (newHeight - a.height) / 2.0f;

            Rect r = a;
            r.xMin -= offsetWidth;
            r.yMin -= offsetHeight;
            r.xMax += offsetWidth;
            r.yMax += offsetHeight;
            return r;
        }

        public static bool Intersects(Rect r1, Rect r2)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
                return false;
            return true;
        }

        public static bool Intersection(Rect r1, Rect r2, out Rect intersection)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
            {
                intersection = new Rect(0, 0, 0, 0);
                return false;
            }

            float left = Mathf.Max(r1.xMin, r2.xMin);
            float top = Mathf.Max(r1.yMin, r2.yMin);

            float right = Mathf.Min(r1.xMax, r2.xMax);
            float bottom = Mathf.Min(r1.yMax, r2.yMax);

            intersection = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        public static bool IntersectsSegment(Rect rect, Vector2 p1, Vector2 p2)
        {
            float minX = Mathf.Min(p1.x, p2.x);
            float maxX = Mathf.Max(p1.x, p2.x);

            if (maxX > rect.xMax)
            {
                maxX = rect.xMax;
            }

            if (minX < rect.xMin)
            {
                minX = rect.xMin;
            }

            if (minX > maxX)
            {
                return false;
            }

            float minY = Mathf.Min(p1.y, p2.y);
            float maxY = Mathf.Max(p1.y, p2.y);

            float dx = p2.x - p1.x;

            if (Mathf.Abs(dx) > 0.0000001f)
            {
                float a = (p2.y - p1.y) / dx;
                float b = p1.y - a * p1.x;
                minY = a * minX + b;
                maxY = a * maxX + b;
            }

            if (minY > maxY)
            {
                float tmp = maxY;
                maxY = minY;
                minY = tmp;
            }

            if (maxY > rect.yMax)
            {
                maxY = rect.yMax;
            }

            if (minY < rect.yMin)
            {
                minY = rect.yMin;
            }

            if (minY > maxY)
            {
                return false;
            }

            return true;
        }

        public static Rect OffsetX(Rect r, float offsetX)
        {
            return Offset(r, offsetX, 0.0f);
        }

        public static Rect Offset(Rect r, float offsetX, float offsetY)
        {
            Rect nr = r;
            nr.xMin += offsetX;
            nr.yMin += offsetY;
            return nr;
        }

        public static Rect Offset(Rect a, Rect b)
        {
            Rect nr = a;
            nr.xMin += b.xMin;
            nr.yMin += b.yMin;
            return nr;
        }

        public static Rect Move(Rect r, Vector2 delta)
        {
            Rect nr = r;
            nr.xMin += delta.x;
            nr.yMin += delta.y;
            nr.xMax += delta.x;
            nr.yMax += delta.y;
            return nr;
        }
    }
}
