using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface IEditablePathController
    {
        IEditablePath editablePath { get; set; }
        IEditablePath closestEditablePath { get; }
        ISnapping<Vector3> snapping { get; set; }
        bool enableSnapping { get; set; }
        void RegisterUndo(string name);
        void ClearSelection();
        void SelectPoint(int index, bool select);
        void CreatePoint(int index, Vector3 position);
        void RemoveSelectedPoints();
        void MoveSelectedPoints(Vector3 delta);
        void MoveEdge(int index, Vector3 delta);
        void SetLeftTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedRightTangent);
        void SetRightTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedLeftTangent);
        void ClearClosestPath();
        void AddClosestPath(float distance);
    }
}
