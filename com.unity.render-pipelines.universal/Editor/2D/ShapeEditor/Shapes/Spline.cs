using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal struct Spline : IShape
    {
        public bool isOpenEnded;

        public Vector3[] points;

        ShapeType IShape.type => ShapeType.Spline;

        bool IShape.isOpenEnded => isOpenEnded;

        ControlPoint[] IShape.ToControlPoints()
        {
            if (points == null)
                throw new NullReferenceException("Points array is null");

            if (!points.IsSpline(isOpenEnded)) 
                throw new Exception("The provided control point array can't conform a Spline.");

            var controlPoints = new List<ControlPoint>();
            var leftTangent = Vector3.zero;
            var rightTangent = Vector3.zero;
            var pointCount = points.Length;

            for (var i = 0; i < pointCount; i += 3)
            {
                if (i == 0)
                {
                    if (isOpenEnded)
                        leftTangent = points[0];
                    else
                        leftTangent = points[EditablePathUtility.Mod(-1, pointCount)];
                }
                
                if (i == pointCount - 1 && isOpenEnded)
                    rightTangent = points[i];
                else
                    rightTangent = points[i+1];


                controlPoints.Add(
                    new ControlPoint()
                    {
                        position = points[i],
                        leftTangent = leftTangent,
                        rightTangent = rightTangent,
                        tangentMode = TangentMode.Broken
                    });

                if (i == pointCount - 1 && isOpenEnded)
                    leftTangent = Vector3.zero;
                else
                    leftTangent = points[i+2];
            }

            pointCount = controlPoints.Count;

            for (var i = 0; i < pointCount; ++i) 
            {
                var prevIndex = EditablePathUtility.Mod(i-1, pointCount);
                var nextIndex = EditablePathUtility.Mod(i+1, pointCount);
                var controlPoint = controlPoints[i];
                var prevControlPoint = controlPoints[prevIndex];
                var nextControlPoint = controlPoints[nextIndex];

                var liniarLeftPosition = (prevControlPoint.position - controlPoint.position) / 3f;
                var isLeftTangentLinear = (controlPoint.localLeftTangent - liniarLeftPosition).sqrMagnitude < 0.001f;

                if (isLeftTangentLinear) 
                    controlPoint.localLeftTangent = Vector3.zero;

                var liniarRightPosition = (nextControlPoint.position - controlPoint.position) / 3f;
                var isRightTangentLinear = (controlPoint.localRightTangent - liniarRightPosition).sqrMagnitude < 0.001f;

                if (isRightTangentLinear)
                    controlPoint.localRightTangent = Vector3.zero;

                var tangentDotProduct = Vector3.Dot(controlPoint.localLeftTangent.normalized, controlPoint.localRightTangent.normalized);
                var isContinous = tangentDotProduct < 0f && (tangentDotProduct + 1) * (tangentDotProduct + 1) < 0.001f;

                if (isLeftTangentLinear && isRightTangentLinear)
                    controlPoint.tangentMode = TangentMode.Linear;
                else if (isLeftTangentLinear || isRightTangentLinear)
                    controlPoint.tangentMode = TangentMode.Broken;
                else if (isContinous)
                    controlPoint.tangentMode = TangentMode.Continuous;

                controlPoints[i] = controlPoint;
            }

            return controlPoints.ToArray();
        }

        public static Spline empty = new Spline() { isOpenEnded = true, points = new Vector3[0] };
    }
}
