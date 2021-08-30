using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal static class EditablePathExtensions
    {
        public static Polygon ToPolygon(this IEditablePath path)
        {
            var polygon = new Polygon()
            {
                isOpenEnded = path.isOpenEnded,
                points = new Vector3[path.pointCount]
            };

            for (var i = 0; i < path.pointCount; ++i)
                polygon.points[i] = path.GetPoint(i).position;

            return polygon;
        }

        public static Spline ToSpline(this IEditablePath path)
        {
            var count = path.pointCount * 3;

            if (path.isOpenEnded)
                count -= 2;

            var spline = new Spline()
            {
                isOpenEnded = path.isOpenEnded,
                points = new Vector3[count]
            };

            for (var i = 0; i < path.pointCount; ++i)
            {
                var point = path.GetPoint(i);

                spline.points[i * 3] = point.position;

                if (i * 3 + 1 < count)
                {
                    var nextIndex = EditablePathUtility.Mod(i + 1, path.pointCount);

                    spline.points[i * 3 + 1] = path.CalculateRightTangent(i);
                    spline.points[i * 3 + 2] = path.CalculateLeftTangent(nextIndex);
                }
            }

            return spline;
        }

        public static Vector3 CalculateLocalLeftTangent(this IEditablePath path, int index)
        {
            return path.CalculateLeftTangent(index) - path.GetPoint(index).position;
        }

        public static Vector3 CalculateLeftTangent(this IEditablePath path, int index)
        {
            var point = path.GetPoint(index);
            var isTangentLinear = point.localLeftTangent == Vector3.zero;
            var isEndpoint = path.isOpenEnded && index == 0;
            var tangent = point.leftTangent;

            if (isEndpoint)
                return point.position;

            if (isTangentLinear)
            {
                var prevPoint = path.GetPrevPoint(index);
                var v = prevPoint.position - point.position;
                tangent = point.position + v.normalized * (v.magnitude / 3f);
            }

            return tangent;
        }

        public static Vector3 CalculateLocalRightTangent(this IEditablePath path, int index)
        {
            return path.CalculateRightTangent(index) - path.GetPoint(index).position;
        }

        public static Vector3 CalculateRightTangent(this IEditablePath path, int index)
        {
            var point = path.GetPoint(index);
            var isTangentLinear = point.localRightTangent == Vector3.zero;
            var isEndpoint = path.isOpenEnded && index == path.pointCount - 1;
            var tangent = point.rightTangent;

            if (isEndpoint)
                return point.position;

            if (isTangentLinear)
            {
                var nextPoint = path.GetNextPoint(index);
                var v = nextPoint.position - point.position;
                tangent = point.position + v.normalized * (v.magnitude / 3f);
            }

            return tangent;
        }

        public static ControlPoint GetPrevPoint(this IEditablePath path, int index)
        {
            return path.GetPoint(EditablePathUtility.Mod(index - 1, path.pointCount));
        }

        public static ControlPoint GetNextPoint(this IEditablePath path, int index)
        {
            return path.GetPoint(EditablePathUtility.Mod(index + 1, path.pointCount));
        }

        public static void UpdateTangentMode(this IEditablePath path, int index)
        {
            var localToWorldMatrix = path.localToWorldMatrix;
            path.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = path.GetPoint(index);
            var isLeftTangentLinear = controlPoint.localLeftTangent == Vector3.zero;
            var isRightTangentLinear = controlPoint.localRightTangent == Vector3.zero;

            if (isLeftTangentLinear && isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Linear;
            else if (isLeftTangentLinear || isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Broken;
            else if (controlPoint.tangentMode != TangentMode.Continuous)
                controlPoint.tangentMode = TangentMode.Broken;

            controlPoint.StoreTangents();
            path.SetPoint(index, controlPoint);
            path.localToWorldMatrix = localToWorldMatrix;
        }

        public static void UpdateTangentsFromMode(this IEditablePath path)
        {
            const float kEpsilon = 0.001f;

            var localToWorldMatrix = path.localToWorldMatrix;
            path.localToWorldMatrix = Matrix4x4.identity;

            for (var i = 0; i < path.pointCount; ++i)
            {
                var controlPoint = path.GetPoint(i);

                if (controlPoint.tangentMode == TangentMode.Linear)
                {
                    controlPoint.localLeftTangent = Vector3.zero;
                    controlPoint.localRightTangent = Vector3.zero;
                }
                else if (controlPoint.tangentMode == TangentMode.Broken)
                {
                    var isLeftEndpoint = path.isOpenEnded && i == 0;
                    var prevPoint = path.GetPrevPoint(i);
                    var nextPoint = path.GetNextPoint(i);

                    var liniarLeftPosition = (prevPoint.position - controlPoint.position) / 3f;
                    var isLeftTangentLinear = isLeftEndpoint || (controlPoint.localLeftTangent - liniarLeftPosition).sqrMagnitude < kEpsilon;

                    if (isLeftTangentLinear)
                        controlPoint.localLeftTangent = Vector3.zero;

                    var isRightEndpoint = path.isOpenEnded && i == path.pointCount - 1;
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
                path.SetPoint(i, controlPoint);
            }

            path.localToWorldMatrix = localToWorldMatrix;
        }

        public static void SetTangentMode(this IEditablePath path, int index, TangentMode tangentMode)
        {
            var localToWorldMatrix = path.localToWorldMatrix;
            path.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = path.GetPoint(index);
            var isEndpoint = path.isOpenEnded && (index == 0 || index == path.pointCount - 1);
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
                    var prevPoint = path.GetPrevPoint(index);
                    var nextPoint = path.GetNextPoint(index);
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
                        controlPoint.localLeftTangent = path.CalculateLocalLeftTangent(index);

                    if (isRightLinear)
                        controlPoint.localRightTangent = path.CalculateLocalRightTangent(index);
                }
            }

            controlPoint.StoreTangents();
            path.SetPoint(index, controlPoint);
            path.localToWorldMatrix = localToWorldMatrix;
        }

        public static void MirrorTangent(this IEditablePath path, int index)
        {
            var localToWorldMatrix = path.localToWorldMatrix;
            path.localToWorldMatrix = Matrix4x4.identity;

            var controlPoint = path.GetPoint(index);

            if (controlPoint.tangentMode == TangentMode.Linear)
                return;

            if (!Mathf.Approximately((controlPoint.localLeftTangent + controlPoint.localRightTangent).sqrMagnitude, 0f))
            {
                if (controlPoint.mirrorLeft)
                    controlPoint.localLeftTangent = -controlPoint.localRightTangent;
                else
                    controlPoint.localRightTangent = -controlPoint.localLeftTangent;

                controlPoint.StoreTangents();
                path.SetPoint(index, controlPoint);
            }

            path.localToWorldMatrix = localToWorldMatrix;
        }
    }
}
