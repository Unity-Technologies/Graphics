using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class EditablePathController : IEditablePathController
    {
        private ISnapping<Vector3> m_Snapping = new Snapping();

        public IEditablePath shapeEditor { get; set; }
        public IEditablePath closestShapeEditor { get { return shapeEditor; } }

        public ISnapping<Vector3> snapping
        {
            get { return m_Snapping; }
            set { m_Snapping = value; }
        }

        public bool enableSnapping { get; set; }

        public void RegisterUndo(string name)
        {
            if (shapeEditor.undoObject != null)
                shapeEditor.undoObject.RegisterUndo(name);
        }

        public void ClearSelection()
        {
            shapeEditor.selection.Clear();
        }

        public void SelectPoint(int index, bool select)
        {
            shapeEditor.selection.Select(index, select);
        }

        public void CreatePoint(int index, Vector3 position)
        {
            ClearSelection();

            if (shapeEditor.shapeType == ShapeType.Polygon)
            {
                shapeEditor.InsertPoint(index + 1, new ControlPoint() { position = position });
            }
            else if (shapeEditor.shapeType == ShapeType.Spline)
            {
                var nextIndex = NextIndex(index);
                var currentPoint = shapeEditor.GetPoint(index);
                var nextPoint = shapeEditor.GetPoint(nextIndex);

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

                shapeEditor.SetPoint(index, currentPoint);
                shapeEditor.SetPoint(nextIndex, nextPoint);
                shapeEditor.InsertPoint(newPointIndex, newPoint);
            }
        }

        public void RemoveSelectedPoints()
        {
            var minPointCount = shapeEditor.isOpenEnded ? 2 : 3;

            if (shapeEditor.pointCount > minPointCount)
            {
                var indices = shapeEditor.selection.elements.OrderByDescending( i => i);

                foreach (var index in indices)
                    if (shapeEditor.pointCount > minPointCount)
                        shapeEditor.RemovePoint(index);

                ClearSelection();
            }
        }

        public void MoveSelectedPoints(Vector3 delta)
        {
            for (var i = 0; i < shapeEditor.pointCount; ++i)
            {
                if (shapeEditor.selection.Contains(i))
                {                            
                    var controlPoint = shapeEditor.GetPoint(i);
                    controlPoint.position += delta;
                    shapeEditor.SetPoint(i, controlPoint);
                }
            }
        }

        public void MoveEdge(int index, Vector3 delta)
        {
            if (shapeEditor.isOpenEnded && index == shapeEditor.pointCount - 1)
                return;
            
            var controlPoint = shapeEditor.GetPoint(index);
            controlPoint.position += delta;
            shapeEditor.SetPoint(index, controlPoint);
            controlPoint = NextControlPoint(index);
            controlPoint.position += delta;
            shapeEditor.SetPoint(NextIndex(index), controlPoint);
        }

        public void SetLeftTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedRightTangent)
        {
            var controlPoint = shapeEditor.GetPoint(index);
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

            shapeEditor.SetPoint(index, controlPoint);
        }

        public void SetRightTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedLeftTangent)
        {
            var controlPoint = shapeEditor.GetPoint(index);
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

            shapeEditor.SetPoint(index, controlPoint);
        }

        public void ClearClosestShapeEditor() { }
        public void AddClosestShapeEditor(float distance) { }

        private Vector3 GetLeftTangentPosition(int index)
        {
            var isLinear = Mathf.Approximately(shapeEditor.GetPoint(index).localLeftTangent.sqrMagnitude, 0f);

            if (isLinear)
            {
                var position = shapeEditor.GetPoint(index).position;
                var prevPosition = PrevControlPoint(index).position;

                return (1f / 3f) * (prevPosition - position) + position;
            }

            return shapeEditor.GetPoint(index).leftTangent;
        }

        private Vector3 GetRightTangentPosition(int index)
        {
            var isLinear = Mathf.Approximately(shapeEditor.GetPoint(index).localRightTangent.sqrMagnitude, 0f);

            if (isLinear)
            {
                var position = shapeEditor.GetPoint(index).position;
                var nextPosition = NextControlPoint(index).position;

                return (1f / 3f) * (nextPosition - position) + position;
            }

            return shapeEditor.GetPoint(index).rightTangent;
        }

        private int NextIndex(int index)
        {
            return EditablePathUtility.Mod(index + 1, shapeEditor.pointCount);
        }

        private ControlPoint NextControlPoint(int index)
        {
            return shapeEditor.GetPoint(NextIndex(index));
        }

        private int PrevIndex(int index)
        {
            return EditablePathUtility.Mod(index - 1, shapeEditor.pointCount);
        }

        private ControlPoint PrevControlPoint(int index)
        {
            return shapeEditor.GetPoint(PrevIndex(index));
        }
    }
}
