using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D
{
    [Serializable]
    internal class EditablePath : IEditablePath
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

        public Matrix4x4 worldToLocalMatrix
        {
            get { return m_WorldToLocalMatrix; }
        }

        public bool isOpenEnded
        {
            get
            {
                if (pointCount < 3)
                    return true;

                return m_IsOpenEnded;
            }
            set { m_IsOpenEnded = value; }
        }

        public ISelection<int> selection
        {
            get { return m_Selection; }
        }

        public int pointCount
        {
            get { return m_ControlPoints.Count; }
        }

        public ControlPoint GetPoint(int index)
        {
            return TransformPoint(localToWorldMatrix, m_ControlPoints[index]);
        }

        public void SetPoint(int index, ControlPoint controlPoint)
        {
            m_ControlPoints[index] = TransformPoint(worldToLocalMatrix, controlPoint);
        }

        public void AddPoint(ControlPoint controlPoint)
        {
            m_ControlPoints.Insert(pointCount, TransformPoint(worldToLocalMatrix, controlPoint));
        }

        public void InsertPoint(int index, ControlPoint controlPoint)
        {
            m_ControlPoints.Insert(index, TransformPoint(worldToLocalMatrix, controlPoint));
        }

        public void RemovePoint(int index)
        {
            m_ControlPoints.RemoveAt(index);
        }

        public void Clear()
        {
            m_ControlPoints.Clear();
        }

        private ControlPoint TransformPoint(Matrix4x4 transformMatrix, ControlPoint controlPoint)
        {
            if (transformMatrix == Matrix4x4.identity)
                return controlPoint;

            var newControlPoint = new ControlPoint()
            {
                position = transformMatrix.MultiplyPoint3x4(controlPoint.position),
                tangentMode = controlPoint.tangentMode,
                continuousCache = controlPoint.continuousCache,
                brokenCache = controlPoint.brokenCache,
                mirrorLeft = controlPoint.mirrorLeft
            };

            newControlPoint.rightTangent = transformMatrix.MultiplyPoint3x4(controlPoint.rightTangent);
            newControlPoint.leftTangent = transformMatrix.MultiplyPoint3x4(controlPoint.leftTangent);

            return newControlPoint;
        }

        public bool Select(ISelector<Vector3> selector)
        {
            var changed = false;

            for (var i = 0; i < pointCount; ++i)
                changed |= selection.Select(i, selector.Select(GetPoint(i).position));

            return changed;
        }

        public virtual void SetDefaultShape()
        {
        }
    }
}
