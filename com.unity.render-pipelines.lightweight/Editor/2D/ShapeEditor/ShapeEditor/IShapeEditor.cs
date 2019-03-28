using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface IShapeEditor : ISelectable<Vector3>
    {
        ShapeType shapeType { get; set; }
        IUndoObject undoObject { get; set; }
        ISelection<int> pointSelection { get; }
        Matrix4x4 localToWorldMatrix { get; set; }
        Vector3 forward { get; set; }
        Vector3 up { get; set; }
        Vector3 right { get; set; }
        bool snapping { get; set; }
        bool isOpenEnded { get; set; }
        int pointCount { get; }
        ControlPoint GetPoint(int index);
        void SetPoint(int index, ControlPoint controlPoint);
        void AddPoint(ControlPoint controlPoint);
        void InsertPoint(int index, ControlPoint controlPoint);
        void RemovePoint(int index);
        void UpdateTangentMode(int index);
        void SetTangentMode(int index, TangentMode tangentMode);
        void Clear();
    }
}
