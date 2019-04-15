using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal struct Polygon : IShape
    {
        public bool isOpenEnded;

        public Vector3[] points;

        ShapeType IShape.type => ShapeType.Polygon;

        bool IShape.isOpenEnded => isOpenEnded;

        ControlPoint[] IShape.ToControlPoints()
        {
            if (points == null)
                throw new NullReferenceException("Points array is null");

            var controlPoints = new List<ControlPoint>();

            foreach (var point in points)
            {
                controlPoints.Add(new ControlPoint() { position = point });
            }

            return controlPoints.ToArray();
        }

        public static Polygon empty = new Polygon() { isOpenEnded = true, points = new Vector3[0] };
    }
}
