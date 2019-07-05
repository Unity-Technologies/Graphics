using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface IEditablePath : ISelectable<Vector3>
    {
        ShapeType shapeType { get; set; }
        IUndoObject undoObject { get; set; }
        ISelection<int> selection { get; }
        Matrix4x4 localToWorldMatrix { get; set; }
        Vector3 forward { get; set; }
        Vector3 up { get; set; }
        Vector3 right { get; set; }
        bool isOpenEnded { get; set; }
        int pointCount { get; }
        ControlPoint GetPoint(int index);
        void SetPoint(int index, ControlPoint controlPoint);
        void AddPoint(ControlPoint controlPoint);
        void InsertPoint(int index, ControlPoint controlPoint);
        void RemovePoint(int index);
        void Clear();
    }
}
