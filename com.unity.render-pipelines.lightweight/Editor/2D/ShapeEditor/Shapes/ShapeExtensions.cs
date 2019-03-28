using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal static class ShapeExtensions
    {
        public static Polygon ToPolygon(this Vector3[] points, bool isOpenEnded)
        {
           return new Polygon()
           {
               isOpenEnded = isOpenEnded,
               points = points
            };
        }

        public static IShape ToSpline(this Vector3[] points, bool isOpenEnded)
        {
            if (points.Length < 4 || (isOpenEnded && points.Length % 3 != 1) || (!isOpenEnded && points.Length % 3 != 0)) 
                return points.ToPolygon(isOpenEnded).ToSpline();

            return new Spline()
            {
                isOpenEnded = isOpenEnded,
                points = points
            };
        }

        public static IShape ToSpline(this Polygon polygon)
        {
            var newPointCount = polygon.points.Length * 3;

            if (polygon.isOpenEnded)
                newPointCount = (polygon.points.Length - 1) * 3 + 1;

            var newPoints = new Vector3[newPointCount];
            var controlPoints = polygon.points;
            var pointCount = controlPoints.Length;

            for (var i = 0; i < pointCount; ++i)
            {
                var nextIndex = (i + 1) % pointCount;
                var point = controlPoints[i];
                var v = controlPoints[nextIndex] - point;

                newPoints[i * 3] = point;

                if (i * 3 + 2 < newPointCount)
                {
                    newPoints[i * 3 + 1] = point + v / 3f;
                    newPoints[i * 3 + 2] = point + v * 2f / 3f;
                }
            }

            return new Spline()
            {
                isOpenEnded = polygon.isOpenEnded,
                points = newPoints
            };
        }
    }
}
