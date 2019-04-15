using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal static class EditablePathExtensions
    {
        public static Polygon ToPolygon(this IEditablePath shapeEditor)
        {
            var polygon = new Polygon()
            {
               isOpenEnded = shapeEditor.isOpenEnded,
               points = new Vector3[shapeEditor.pointCount]
            };

            for (var i = 0; i < shapeEditor.pointCount; ++i)
                polygon.points[i] = shapeEditor.GetPoint(i).position;

            return polygon;
        }

        public static Spline ToSpline(this IEditablePath shapeEditor)
        {
            var count = shapeEditor.pointCount * 3;

            if (shapeEditor.isOpenEnded)
                count -= 2;
            
            var spline = new Spline()
            {
               isOpenEnded = shapeEditor.isOpenEnded,
               points = new Vector3[count]
            };

            for (var i = 0; i < shapeEditor.pointCount; ++i)
            {
                var point = shapeEditor.GetPoint(i);

                spline.points[i*3] = point.position;

                if (i * 3 + 1 < count)
                {
                    var nextIndex = EditablePathUtility.Mod(i+1, shapeEditor.pointCount);

                    spline.points[i*3 + 1] = shapeEditor.CalculateRightTangent(i);
                    spline.points[i*3 + 2] = shapeEditor.CalculateLeftTangent(nextIndex);
                }
            }

            return spline;
        }

        public static Vector3 CalculateLocalLeftTangent(this IEditablePath shapeEditor, int index)
        {
            return shapeEditor.CalculateLeftTangent(index) - shapeEditor.GetPoint(index).position;
        }

        public static Vector3 CalculateLeftTangent(this IEditablePath shapeEditor, int index)
        {
            var point = shapeEditor.GetPoint(index);
            var isTangentLinear = point.localLeftTangent == Vector3.zero;
            var isEndpoint = shapeEditor.isOpenEnded && index == 0;
            var tangent = point.leftTangent;

            if (isEndpoint)
                return point.position;

            if (isTangentLinear)
            {
                var prevPoint = shapeEditor.GetPrevPoint(index);
                var v = prevPoint.position - point.position;
                tangent = point.position + v.normalized * (v.magnitude / 3f);
            }

            return tangent;
        }

        public static Vector3 CalculateLocalRightTangent(this IEditablePath shapeEditor, int index)
        {
            return shapeEditor.CalculateRightTangent(index) - shapeEditor.GetPoint(index).position;
        }

        public static Vector3 CalculateRightTangent(this IEditablePath shapeEditor, int index)
        {
            var point = shapeEditor.GetPoint(index);
            var isTangentLinear = point.localRightTangent == Vector3.zero;
            var isEndpoint = shapeEditor.isOpenEnded && index == shapeEditor.pointCount - 1;
            var tangent = point.rightTangent;

            if (isEndpoint)
                return point.position;
            
            if (isTangentLinear)
            {
                var nextPoint = shapeEditor.GetNextPoint(index);
                var v = nextPoint.position - point.position;
                tangent = point.position + v.normalized * (v.magnitude / 3f);
            }

            return tangent;
        }

        public static ControlPoint GetPrevPoint(this IEditablePath shapeEditor, int index)
        {
            return shapeEditor.GetPoint(EditablePathUtility.Mod(index - 1, shapeEditor.pointCount));
        }

        public static ControlPoint GetNextPoint(this IEditablePath shapeEditor, int index)
        {
            return shapeEditor.GetPoint(EditablePathUtility.Mod(index + 1, shapeEditor.pointCount));
        }

        public static void UpdateTangentMode(this IEditablePath shapeEditor, int index)
        {
            var localToWorldMatrix = shapeEditor.localToWorldMatrix;
            shapeEditor.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = shapeEditor.GetPoint(index);
            var isLeftTangentLinear = controlPoint.localLeftTangent == Vector3.zero;
            var isRightTangentLinear = controlPoint.localRightTangent == Vector3.zero;

            if (isLeftTangentLinear && isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Linear;
            else if (isLeftTangentLinear || isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Broken;
            else if (controlPoint.tangentMode != TangentMode.Continuous)
                controlPoint.tangentMode = TangentMode.Broken;
            
            controlPoint.StoreTangents();
            shapeEditor.SetPoint(index, controlPoint);
            shapeEditor.localToWorldMatrix = localToWorldMatrix;
        }

        public static void UpdateTangentsFromMode(this IEditablePath shapeEditor)
        {
            const float kEpsilon = 0.001f;

            var localToWorldMatrix = shapeEditor.localToWorldMatrix;
            shapeEditor.localToWorldMatrix = Matrix4x4.identity;

            for (var i = 0; i < shapeEditor.pointCount; ++i)
            {
                var controlPoint = shapeEditor.GetPoint(i);
                 
                if (controlPoint.tangentMode == TangentMode.Linear)
                {
                    controlPoint.localLeftTangent = Vector3.zero;
                    controlPoint.localRightTangent = Vector3.zero;
                }
                else if (controlPoint.tangentMode == TangentMode.Broken)
                {
                    var isLeftEndpoint = shapeEditor.isOpenEnded && i == 0;
                    var prevPoint = shapeEditor.GetPrevPoint(i);
                    var nextPoint = shapeEditor.GetNextPoint(i);

                    var liniarLeftPosition = (prevPoint.position - controlPoint.position) / 3f;
                    var isLeftTangentLinear = isLeftEndpoint || (controlPoint.localLeftTangent - liniarLeftPosition).sqrMagnitude < kEpsilon;

                    if (isLeftTangentLinear) 
                        controlPoint.localLeftTangent = Vector3.zero;

                    var isRightEndpoint = shapeEditor.isOpenEnded && i == shapeEditor.pointCount-1;
                    var liniarRightPosition = (nextPoint.position - controlPoint.position) / 3f;
                    var isRightTangentLinear = isRightEndpoint || (controlPoint.localRightTangent - liniarRightPosition).sqrMagnitude < kEpsilon;

                    if (isRightTangentLinear)
                        controlPoint.localRightTangent = Vector3.zero;

                    if (isLeftTangentLinear && isRightTangentLinear)
                        controlPoint.tangentMode = TangentMode.Linear;
                }
                else if (controlPoint.tangentMode == TangentMode.Continuous)
                {
                    //TODO: ensure tangent continuity
                }

                controlPoint.StoreTangents();
                shapeEditor.SetPoint(i, controlPoint);
            }

            shapeEditor.localToWorldMatrix = localToWorldMatrix;
        }

        public static void SetTangentMode(this IEditablePath shapeEditor, int index, TangentMode tangentMode)
        {
            var localToWorldMatrix = shapeEditor.localToWorldMatrix;
            shapeEditor.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = shapeEditor.GetPoint(index);
            var isEndpoint = shapeEditor.isOpenEnded && (index == 0 || index == shapeEditor.pointCount - 1);
            var oldTangentMode = controlPoint.tangentMode;

            controlPoint.tangentMode = tangentMode;
            controlPoint.RestoreTangents();

            if (tangentMode == TangentMode.Linear)
            {
                controlPoint.localLeftTangent = Vector3.zero;
                controlPoint.localRightTangent = Vector3.zero;
            }
            else if (tangentMode == TangentMode.Continuous && !isEndpoint)
            {
                var isLeftLinear = controlPoint.localLeftTangent == Vector3.zero;
                var isRightLinear = controlPoint.localRightTangent == Vector3.zero;
                var tangentDotProduct = Vector3.Dot(controlPoint.localLeftTangent.normalized, controlPoint.localRightTangent.normalized);
                var isContinous = tangentDotProduct < 0f && (tangentDotProduct + 1) < 0.001f;
                var isLinear = isLeftLinear && isRightLinear;

                if ((isLinear || oldTangentMode == TangentMode.Broken) && !isContinous)
                {
                    var prevPoint = shapeEditor.GetPrevPoint(index);
                    var nextPoint = shapeEditor.GetNextPoint(index);
                    var vLeft = prevPoint.position - controlPoint.position;
                    var vRight = nextPoint.position - controlPoint.position;
                    var rightDirection = Vector3.Cross(Vector3.Cross(vLeft, vRight), vLeft.normalized + vRight.normalized).normalized;
                    var scale = 1f / 3f;

                    if (isLeftLinear)
                        controlPoint.localLeftTangent = vLeft.magnitude * scale * -rightDirection;
                    else
                        controlPoint.localLeftTangent = controlPoint.localLeftTangent.magnitude * -rightDirection;

                    if (isRightLinear)
                        controlPoint.localRightTangent = vRight.magnitude * scale * rightDirection;
                    else
                        controlPoint.localRightTangent = controlPoint.localRightTangent.magnitude * rightDirection;
                }
            }
            else
            {
                var isLeftLinear = controlPoint.localLeftTangent == Vector3.zero;
                var isRightLinear = controlPoint.localRightTangent == Vector3.zero;
                
                if (isLeftLinear || isRightLinear)
                {
                    if (isLeftLinear)
                        controlPoint.localLeftTangent = shapeEditor.CalculateLocalLeftTangent(index);

                    if (isRightLinear)
                        controlPoint.localRightTangent = shapeEditor.CalculateLocalRightTangent(index);
                }
            }

            controlPoint.StoreTangents();
            shapeEditor.SetPoint(index, controlPoint);
            shapeEditor.localToWorldMatrix = localToWorldMatrix;
        }

        public static void MirrorTangent(this IEditablePath shapeEditor, int index)
        {
            var localToWorldMatrix = shapeEditor.localToWorldMatrix;
            shapeEditor.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = shapeEditor.GetPoint(index);

            if (controlPoint.tangentMode == TangentMode.Linear)
                return;

            if (!Mathf.Approximately((controlPoint.localLeftTangent + controlPoint.localRightTangent).sqrMagnitude, 0f))
            {
                if (controlPoint.mirrorLeft)
                    controlPoint.localLeftTangent = -controlPoint.localRightTangent;
                else
                    controlPoint.localRightTangent = -controlPoint.localLeftTangent;

                controlPoint.StoreTangents();
                shapeEditor.SetPoint(index, controlPoint);
            }

            shapeEditor.localToWorldMatrix = localToWorldMatrix;
        }
    }
}
