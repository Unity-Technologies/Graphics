using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class ScriptableShapeEditor : ScriptableObject, IShapeEditor, IUndoObject
    {
        [SerializeField]
        private UnityEngine.Object m_Owner;
        [SerializeField]
        private ShapeEditor m_ShapeEditor = new ShapeEditor();

        public UnityEngine.Object owner
        {
            get { return m_Owner; }
            internal set { m_Owner = value; }
        }

        public ShapeType shapeType
        {
            get { return m_ShapeEditor.shapeType; }
            set { m_ShapeEditor.shapeType = value; }
        }

        public IUndoObject undoObject
        {
            get { return this; }
            set { }
        }

        public ISelection<int> pointSelection
        {
            get { return m_ShapeEditor.pointSelection; }
        }

        public Matrix4x4 localToWorldMatrix
        {
            get { return m_ShapeEditor.localToWorldMatrix; }
            set { m_ShapeEditor.localToWorldMatrix = value; }
        }

        public Vector3 forward
        {
            get { return m_ShapeEditor.forward; }
            set { m_ShapeEditor.forward = value; }
        }

        public Vector3 up
        {
            get { return m_ShapeEditor.up; }
            set { m_ShapeEditor.up = value; }
        }

        public Vector3 right
        {
            get { return m_ShapeEditor.right; }
            set { m_ShapeEditor.right = value; }
        }

        public bool snapping
        {
            get { return m_ShapeEditor.snapping; }
            set { m_ShapeEditor.snapping = value; }
        }

        public bool isOpenEnded
        {
            get { return m_ShapeEditor.isOpenEnded; }
            set { m_ShapeEditor.isOpenEnded = value; }
        }

        public int pointCount
        {
            get { return m_ShapeEditor.pointCount; }
        }

        public void Select(ISelector<Vector3> selector)
        {
            m_ShapeEditor.Select(selector);
        }

        public void UpdateTangentMode(int index)
        {
            m_ShapeEditor.UpdateTangentMode(index);
        }

        public void SetTangentMode(int index, TangentMode tangentMode)
        {
            m_ShapeEditor.SetTangentMode(index, tangentMode);
        }

        public virtual void Clear()
        {
            m_ShapeEditor.Clear();
        }

        public virtual ControlPoint GetPoint(int index)
        {
            return m_ShapeEditor.GetPoint(index);
        }

        public virtual void SetPoint(int index, ControlPoint controlPoint)
        {
            m_ShapeEditor.SetPoint(index, controlPoint);
        }

        public virtual void AddPoint(ControlPoint controlPoint)
        {
            m_ShapeEditor.AddPoint(controlPoint);
        }

        public virtual void InsertPoint(int index, ControlPoint controlPoint)
        {
            m_ShapeEditor.InsertPoint(index, controlPoint);
        }

        public virtual void RemovePoint(int index)
        {
            m_ShapeEditor.RemovePoint(index);
        }

        void IUndoObject.RegisterUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
        }
    }
}
