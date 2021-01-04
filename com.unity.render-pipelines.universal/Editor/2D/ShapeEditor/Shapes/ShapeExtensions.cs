using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
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

        public static Spline ToSpline(this Vector3[] points, bool isOpenEnded)
        {
            if (!points.IsSpline(isOpenEnded) && points.IsSpline(!isOpenEnded))
            {
                var pointList = new List<Vector3>(points);

                if (isOpenEnded)
                {
                    while (pointList.Count % 3 != 1)
                        pointList.RemoveAt(pointList.Count - 1);

                    points = pointList.ToArray();
                }
                else
                {
                    var last = pointList[pointList.Count - 1];
                    var first = pointList[0];
                    var v = first - last;

                    pointList.Add(last + v.normalized * (v.magnitude / 3f));
                    pointList.Add(first - v.normalized * (v.magnitude / 3f));

                    points = pointList.ToArray();
                }
            }

            if (!points.IsSpline(isOpenEnded))
                throw new Exception("The provided control point array can't conform a Spline.");

            return new Spline()
            {
                isOpenEnded = isOpenEnded,
                points = points
            };
        }

        public static bool IsSpline(this Vector3[] points, bool isOpenEnded)
        {
            if (points.Length < 4)
                return false;

            if (isOpenEnded && points.Length % 3 != 1)
                return false;

            if (!isOpenEnded && points.Length % 3 != 0)
                return false;

            return true;
        }

        public static Spline ToSpline(this Polygon polygon)
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
