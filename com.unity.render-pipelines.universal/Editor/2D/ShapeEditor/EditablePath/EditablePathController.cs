using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal class EditablePathController : IEditablePathController
    {
        private ISnapping<Vector3> m_Snapping = new Snapping();

        public IEditablePath editablePath { get; set; }
        public IEditablePath closestEditablePath { get { return editablePath; } }

        public ISnapping<Vector3> snapping
        {
            get { return m_Snapping; }
            set { m_Snapping = value; }
        }

        public bool enableSnapping { get; set; }

        public void RegisterUndo(string name)
        {
            if (editablePath.undoObject != null)
                editablePath.undoObject.RegisterUndo(name);
        }

        public void ClearSelection()
        {
            editablePath.selection.Clear();
        }

        public void SelectPoint(int index, bool select)
        {
            editablePath.selection.Select(index, select);
        }

        public void CreatePoint(int index, Vector3 position)
        {
            ClearSelection();

            if (editablePath.shapeType == ShapeType.Polygon)
            {
                editablePath.InsertPoint(index + 1, new ControlPoint() { position = position });
            }
            else if (editablePath.shapeType == ShapeType.Spline)
            {
                var nextIndex = NextIndex(index);
                var currentPoint = editablePath.GetPoint(index);
                var nextPoint = editablePath.GetPoint(nextIndex);

                float t;
                var closestPoint = BezierUtility.ClosestPointOnCurve(
                    position,
                    currentPoint.position,
                    nextPoint.position,
                    GetRightTangentPosition(index),
                    GetLeftTangentPosition(nextIndex),
                    out t);

                Vector3 leftStartPosition;
                Vector3 leftEndPosition;
                Vector3 leftStartTangent;
                Vector3 leftEndTangent;

                Vector3 rightStartPosition;
                Vector3 rightEndPosition;
                Vector3 rightStartTangent;
                Vector3 rightEndTangent;

                BezierUtility.SplitBezier(t, currentPoint.position, nextPoint.position, GetRightTangentPosition(index), GetLeftTangentPosition(nextIndex),
                    out leftStartPosition, out leftEndPosition, out leftStartTangent, out leftEndTangent,
                    out rightStartPosition, out rightEndPosition, out rightStartTangent, out rightEndTangent);

                var newPointIndex = index + 1;
                var newPoint = new ControlPoint()
                {
                    position = closestPoint,
                    leftTangent = leftEndTangent,
                    rightTangent = rightStartTangent,
                    tangentMode = TangentMode.Continuous
                };

                currentPoint.rightTangent = leftStartTangent;
                nextPoint.leftTangent = rightEndTangent;

                if (currentPoint.tangentMode == TangentMode.Linear && nextPoint.tangentMode == TangentMode.Linear)
                {
                    newPoint.tangentMode = TangentMode.Linear;
                    newPoint.localLeftTangent = Vector3.zero;
                    newPoint.localRightTangent = Vector3.zero;
                    currentPoint.localRightTangent = Vector3.zero;
                    nextPoint.localLeftTangent = Vector3.zero;
                }
                else
                {
                    if (currentPoint.tangentMode == TangentMode.Linear)
                        currentPoint.tangentMode = TangentMode.Broken;

                    if (nextPoint.tangentMode == TangentMode.Linear)
                        nextPoint.tangentMode = TangentMode.Broken;
                }

                editablePath.SetPoint(index, currentPoint);
                editablePath.SetPoint(nextIndex, nextPoint);
                editablePath.InsertPoint(newPointIndex, newPoint);
            }
        }

        public void RemoveSelectedPoints()
        {
            var minPointCount = editablePath.isOpenEnded ? 2 : 3;
            int pointsCountToRemove = editablePath.selection.Count;

            if (editablePath.pointCount != pointsCountToRemove)
            {
                var indices = editablePath.selection.elements.OrderByDescending(i => i);

                foreach (var index in indices)
                {
                    if (editablePath.pointCount > minPointCount)
                    {
                        editablePath.RemovePoint(index);
                    }
                }
                ClearSelection();
            }
            else
            {
                editablePath.SetDefaultShape();
                ClearSelection();
            }
        }

        public void MoveSelectedPoints(Vector3 delta)
        {
            delta = Vector3.ProjectOnPlane(delta, editablePath.forward);

            for (var i = 0; i < editablePath.pointCount; ++i)
            {
                if (editablePath.selection.Contains(i))
                {
                    var controlPoint = editablePath.GetPoint(i);
                    controlPoint.position += delta;
                    editablePath.SetPoint(i, controlPoint);
                }
            }
        }

        public void MoveEdge(int index, Vector3 delta)
        {
            if (editablePath.isOpenEnded && index == editablePath.pointCount - 1)
                return;

            var controlPoint = editablePath.GetPoint(index);
            controlPoint.position += delta;
            editablePath.SetPoint(index, controlPoint);
            controlPoint = NextControlPoint(index);
            controlPoint.position += delta;
            editablePath.SetPoint(NextIndex(index), controlPoint);
        }

        public void SetLeftTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedRightTangent)
        {
            var controlPoint = editablePath.GetPoint(index);
            controlPoint.leftTangent = position;
            controlPoint.mirrorLeft = false;

            if (setToLinear)
            {
                controlPoint.leftTangent = controlPoint.position;
                controlPoint.rightTangent = cachedRightTangent;
            }
            else if (controlPoint.tangentMode == TangentMode.Continuous || mirror)
            {
                var magnitude = controlPoint.localRightTangent.magnitude;

                if (mirror)
                    magnitude = controlPoint.localLeftTangent.magnitude;

                controlPoint.localRightTangent = magnitude * -controlPoint.localLeftTangent.normalized;
            }

            editablePath.SetPoint(index, controlPoint);
        }

        public void SetRightTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedLeftTangent)
        {
            var controlPoint = editablePath.GetPoint(index);
            controlPoint.rightTangent = position;
            controlPoint.mirrorLeft = true;

            if (setToLinear)
            {
                controlPoint.rightTangent = controlPoint.position;
                controlPoint.leftTangent = cachedLeftTangent;
            }
            else if (controlPoint.tangentMode == TangentMode.Continuous || mirror)
            {
                var magnitude = controlPoint.localLeftTangent.magnitude;

                if (mirror)
                    magnitude = controlPoint.localRightTangent.magnitude;

                controlPoint.localLeftTangent = magnitude * -controlPoint.localRightTangent.normalized;
            }

            editablePath.SetPoint(index, controlPoint);
        }

        public void ClearClosestPath() {}
        public void AddClosestPath(float distance) {}

        private Vector3 GetLeftTangentPosition(int index)
        {
            var isLinear = Mathf.Approximately(editablePath.GetPoint(index).localLeftTangent.sqrMagnitude, 0f);

            if (isLinear)
            {
                var position = editablePath.GetPoint(index).position;
                var prevPosition = PrevControlPoint(index).position;

                return (1f / 3f) * (prevPosition - position) + position;
            }

            return editablePath.GetPoint(index).leftTangent;
        }

        private Vector3 GetRightTangentPosition(int index)
        {
            var isLinear = Mathf.Approximately(editablePath.GetPoint(index).localRightTangent.sqrMagnitude, 0f);

            if (isLinear)
            {
                var position = editablePath.GetPoint(index).position;
                var nextPosition = NextControlPoint(index).position;

                return (1f / 3f) * (nextPosition - position) + position;
            }

            return editablePath.GetPoint(index).rightTangent;
        }

        private int NextIndex(int index)
        {
            return EditablePathUtility.Mod(index + 1, editablePath.pointCount);
        }

        private ControlPoint NextControlPoint(int index)
        {
            return editablePath.GetPoint(NextIndex(index));
        }

        private int PrevIndex(int index)
        {
            return EditablePathUtility.Mod(index - 1, editablePath.pointCount);
        }

        private ControlPoint PrevControlPoint(int index)
        {
            return editablePath.GetPoint(PrevIndex(index));
        }
    }
}
