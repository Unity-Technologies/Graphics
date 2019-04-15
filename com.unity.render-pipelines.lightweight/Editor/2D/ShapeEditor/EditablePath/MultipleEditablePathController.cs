using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class MultipleEditablePathController : IEditablePathController
    {
        private IEditablePathController m_Controller = new EditablePathController();
        private List<IEditablePath> m_ShapeEditors = new List<IEditablePath>();
        private float m_ClosestDistance = float.MaxValue;
        private IEditablePath m_ClosestShapeEditor;

        public IEditablePath shapeEditor
        {
            get { return m_Controller.shapeEditor; }
            set { m_Controller.shapeEditor = value; }
        }

        public IEditablePath closestShapeEditor { get; private set; }

        public ISnapping<Vector3> snapping
        {
            get { return m_Controller.snapping; }
            set { m_Controller.snapping = value; }
        }

        public bool enableSnapping
        {
            get { return m_Controller.enableSnapping; }
            set { m_Controller.enableSnapping = value; }
        }

        public void ClearShapeEditors()
        {
            m_ShapeEditors.Clear();
        }

        public void AddShapeEditor(IEditablePath shapeEditor)
        {
            if (!m_ShapeEditors.Contains(shapeEditor))
                m_ShapeEditors.Add(shapeEditor);
        }

        public void RemoveShapeEditor(IEditablePath shapeEditor)
        {
            m_ShapeEditors.Remove(shapeEditor);
        }

        public void RegisterUndo(string name)
        {
            var current = shapeEditor;

            ForEach((s) =>
            {
                shapeEditor = s;
                m_Controller.RegisterUndo(name);
            });

            shapeEditor = current;
        }

        public void ClearSelection()
        {
            var current = shapeEditor;

            ForEach((s) =>
            {
                shapeEditor = s;
                m_Controller.ClearSelection();
            });   

            shapeEditor = current;
        }

        public void SelectPoint(int index, bool select)
        {
            m_Controller.SelectPoint(index, select);
        }

        public void CreatePoint(int index, Vector3 position)
        {
            m_Controller.CreatePoint(index, position);
        }

        public void RemoveSelectedPoints()
        {
            var current = shapeEditor;

            ForEach((s) =>
            {
                shapeEditor = s;
                m_Controller.RemoveSelectedPoints();
            });

            shapeEditor = current;
        }

        public void MoveSelectedPoints(Vector3 delta)
        {
            var current = shapeEditor;

            ForEach((s) =>
            {
                shapeEditor = s;
                var localDelta = Vector3.Scale(s.right + s.up, delta);
                
                m_Controller.MoveSelectedPoints(localDelta);
            });

            shapeEditor = current;
        }

        public void MoveEdge(int index, Vector3 delta)
        {
            m_Controller.MoveEdge(index, delta);
        }

        public void SetLeftTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedRightTangent)
        {
            m_Controller.SetLeftTangent(index, position, setToLinear, mirror, cachedRightTangent);
        }

        public void SetRightTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedLeftTangent)
        {
            m_Controller.SetRightTangent(index, position, setToLinear, mirror, cachedLeftTangent);
        }

        public void ClearClosestShapeEditor()
        {
            m_ClosestDistance = float.MaxValue;
            closestShapeEditor = null;
        }

        public void AddClosestShapeEditor(float distance)
        {
            if (distance <= m_ClosestDistance)
            {
                m_ClosestDistance = distance;
                closestShapeEditor = shapeEditor;
            }
        }

        private void ForEach(Action<IEditablePath> action)
        {
            foreach(var shapeEditor in m_ShapeEditors)
            {
                if (shapeEditor == null)
                    continue;

                action(shapeEditor);
            }
        }
    }
}
