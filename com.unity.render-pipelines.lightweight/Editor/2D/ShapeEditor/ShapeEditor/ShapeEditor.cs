using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    [Serializable]
    internal class ShapeEditor : IShapeEditor
    {
        [SerializeField]
        private ShapeType m_ShapeType;
        [SerializeField]
        private IndexedSelection m_Selection = new IndexedSelection();
        [SerializeField]
        private List<ControlPoint> m_ControlPoints = new List<ControlPoint>();
        [SerializeField]
        private bool m_IsOpenEnded;
        private Matrix4x4 m_LocalToWorldMatrix = Matrix4x4.identity;
        private Matrix4x4 m_WorldToLocalMatrix = Matrix4x4.identity;
        private Vector3 m_Forward = Vector3.forward;
        private Vector3 m_Up = Vector3.up;
        private Vector3 m_Right = Vector3.right;
        private bool m_Snapping = false;

        public ShapeType shapeType
        {
            get { return m_ShapeType; }
            set { m_ShapeType = value; }
        }

        public IUndoObject undoObject { get; set; }
        
        public Matrix4x4 localToWorldMatrix
        {
            get { return m_LocalToWorldMatrix; }
            set
            {
                m_LocalToWorldMatrix = value;
                m_WorldToLocalMatrix = value.inverse;
            }
        }

        public Vector3 forward
        {
            get { return m_Forward; }
            set { m_Forward = value; }
        }

        public Vector3 up
        {
            get { return m_Up; }
            set { m_Up = value; }
        }

        public Vector3 right
        {
            get { return m_Right; }
            set { m_Right = value; }
        }

        public bool snapping
        {
            get { return m_Snapping; }
            set { m_Snapping = value; }
        }

        public Matrix4x4 worldToLocalMatrix
        {
            get { return m_WorldToLocalMatrix; }
        }

        public bool isOpenEnded
        {
            get { return m_IsOpenEnded; }
            set { m_IsOpenEnded = value; }
        }

        public ISelection<int> pointSelection
        {
            get { return m_Selection; }
        }

        public int pointCount
        {
            get { return m_ControlPoints.Count; }
        }

        public ControlPoint GetPoint(int index)
        {
            return LocalToWorld(m_ControlPoints[index]);
        }

        public void SetPoint(int index, ControlPoint controlPoint)
        {
            m_ControlPoints[index] = WorldToLocal(controlPoint);
        }

        public void AddPoint(ControlPoint controlPoint)
        {
            m_ControlPoints.Insert(pointCount, WorldToLocal(controlPoint));
        }

        public void InsertPoint(int index, ControlPoint controlPoint)
        {
            m_ControlPoints.Insert(index, WorldToLocal(controlPoint));
        }

        public void RemovePoint(int index)
        {
            m_ControlPoints.RemoveAt(index);
        }

        public void UpdateTangentMode(int index)
        {
            var controlPoint = m_ControlPoints[index];
            var isLeftTangentLinear = controlPoint.localLeftTangent == Vector3.zero;
            var isRightTangentLinear = controlPoint.localRightTangent == Vector3.zero;

            if (isLeftTangentLinear && isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Linear;
            else if (isLeftTangentLinear || isRightTangentLinear)
                controlPoint.tangentMode = TangentMode.Broken;
            
            controlPoint.StoreTangents();
            m_ControlPoints[index] = controlPoint;
        }

        public void SetTangentMode(int index, TangentMode tangentMode)
        {
            var controlPoint = m_ControlPoints[index];
            var isEndpoint = isOpenEnded && (index == 0 || index == pointCount - 1);
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
                    var prevPoint = m_ControlPoints[ShapeEditorUtility.Mod(index - 1, pointCount)];
                    var nextPoint = m_ControlPoints[ShapeEditorUtility.Mod(index + 1, pointCount)];
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
                    var scale = 1f / 3f;

                    if (isLeftLinear)
                    {
                        var prevPoint = m_ControlPoints[ShapeEditorUtility.Mod(index - 1, pointCount)];
                        var v = prevPoint.position - controlPoint.position;

                        controlPoint.localLeftTangent = v * scale;
                    }

                    if (isRightLinear)
                    {
                        var nextPoint = m_ControlPoints[ShapeEditorUtility.Mod(index + 1, pointCount)];
                        var v = nextPoint.position - controlPoint.position;

                        controlPoint.localRightTangent = v * scale;
                    }
                }
            }

            controlPoint.StoreTangents();
            m_ControlPoints[index] = controlPoint;
        }

        public void Clear()
        {
            m_ControlPoints.Clear();
        }

        private ControlPoint LocalToWorld(ControlPoint controlPoint)
        {
            var newControlPoint = new ControlPoint()
            {
                position = localToWorldMatrix.MultiplyPoint3x4(controlPoint.position),
                tangentMode = controlPoint.tangentMode,
                continuousCache = controlPoint.continuousCache,
                brokenCache = controlPoint.brokenCache
            };

            newControlPoint.rightTangent = localToWorldMatrix.MultiplyPoint3x4(controlPoint.rightTangent);
            newControlPoint.leftTangent = localToWorldMatrix.MultiplyPoint3x4(controlPoint.leftTangent);

            return newControlPoint;
        }

        private ControlPoint WorldToLocal(ControlPoint controlPoint)
        {
            var newControlPoint = new ControlPoint()
            {
                position = worldToLocalMatrix.MultiplyPoint3x4(controlPoint.position),
                tangentMode = controlPoint.tangentMode,
                continuousCache = controlPoint.continuousCache,
                brokenCache = controlPoint.brokenCache
            };

            newControlPoint.rightTangent = worldToLocalMatrix.MultiplyPoint3x4(controlPoint.rightTangent);
            newControlPoint.leftTangent = worldToLocalMatrix.MultiplyPoint3x4(controlPoint.leftTangent);
            
            return newControlPoint;
        }

        public void Select(ISelector<Vector3> selector)
        {
            for (var i = 0; i < pointCount; ++i)
                pointSelection.Select(i, selector.Select(GetPoint(i).position));
        }
    }
}
