using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class MultipleEditablePathController : IEditablePathController
    {
        private IEditablePathController m_Controller = new EditablePathController();
        private List<IEditablePath> m_Paths = new List<IEditablePath>();
        private float m_ClosestDistance = float.MaxValue;
        private IEditablePath m_ClosestPath;

        public IEditablePath editablePath
        {
            get { return m_Controller.editablePath; }
            set { m_Controller.editablePath = value; }
        }

        public IEditablePath closestEditablePath { get; private set; }

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

        public void ClearPaths()
        {
            m_Paths.Clear();
        }

        public void AddPath(IEditablePath path)
        {
            if (!m_Paths.Contains(path))
                m_Paths.Add(path);
        }

        public void RemovePath(IEditablePath path)
        {
            m_Paths.Remove(path);
        }

        public void RegisterUndo(string name)
        {
            var current = editablePath;

            ForEach((s) =>
            {
                editablePath = s;
                m_Controller.RegisterUndo(name);
            });

            editablePath = current;
        }

        public void ClearSelection()
        {
            var current = editablePath;

            ForEach((s) =>
            {
                editablePath = s;
                m_Controller.ClearSelection();
            });   

            editablePath = current;
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
            var current = editablePath;

            ForEach((s) =>
            {
                editablePath = s;
                m_Controller.RemoveSelectedPoints();
            });

            editablePath = current;
        }

        public void MoveSelectedPoints(Vector3 delta)
        {
            var current = editablePath;

            ForEach((s) =>
            {
                editablePath = s;
                m_Controller.MoveSelectedPoints(delta);
            });

            editablePath = current;
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

        public void ClearClosestPath()
        {
            m_ClosestDistance = float.MaxValue;
            closestEditablePath = null;
        }

        public void AddClosestPath(float distance)
        {
            if (distance <= m_ClosestDistance)
            {
                m_ClosestDistance = distance;
                closestEditablePath = editablePath;
            }
        }

        private void ForEach(Action<IEditablePath> action)
        {
            foreach(var path in m_Paths)
            {
                if (path == null)
                    continue;

                action(path);
            }
        }
    }
}
