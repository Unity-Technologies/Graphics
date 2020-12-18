using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal class ScriptablePath : ScriptableObject, IEditablePath, IUndoObject
    {
        [SerializeField]
        private EditablePath m_EditablePath = new EditablePath();
        [SerializeField]
        private bool m_Modified = false;

        internal bool modified
        {
            get { return m_Modified; }
        }

        internal UnityObject owner { get; set; }

        public ShapeType shapeType
        {
            get { return m_EditablePath.shapeType; }
            set { m_EditablePath.shapeType = value; }
        }

        public IUndoObject undoObject
        {
            get { return this; }
            set {}
        }

        public ISelection<int> selection
        {
            get { return m_EditablePath.selection; }
        }

        public Matrix4x4 localToWorldMatrix
        {
            get { return m_EditablePath.localToWorldMatrix; }
            set { m_EditablePath.localToWorldMatrix = value; }
        }

        public Vector3 forward
        {
            get { return m_EditablePath.forward; }
            set { m_EditablePath.forward = value; }
        }

        public Vector3 up
        {
            get { return m_EditablePath.up; }
            set { m_EditablePath.up = value; }
        }

        public Vector3 right
        {
            get { return m_EditablePath.right; }
            set { m_EditablePath.right = value; }
        }

        public bool isOpenEnded
        {
            get { return m_EditablePath.isOpenEnded; }
            set { m_EditablePath.isOpenEnded = value; }
        }

        public int pointCount
        {
            get { return m_EditablePath.pointCount; }
        }

        public bool Select(ISelector<Vector3> selector)
        {
            return m_EditablePath.Select(selector);
        }

        public virtual void Clear()
        {
            m_EditablePath.Clear();
        }

        public virtual ControlPoint GetPoint(int index)
        {
            return m_EditablePath.GetPoint(index);
        }

        public virtual void SetPoint(int index, ControlPoint controlPoint)
        {
            m_EditablePath.SetPoint(index, controlPoint);
        }

        public virtual void AddPoint(ControlPoint controlPoint)
        {
            m_EditablePath.AddPoint(controlPoint);
        }

        public virtual void InsertPoint(int index, ControlPoint controlPoint)
        {
            m_EditablePath.InsertPoint(index, controlPoint);
        }

        public virtual void RemovePoint(int index)
        {
            m_EditablePath.RemovePoint(index);
        }

        void IUndoObject.RegisterUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
            m_Modified = true;
        }

        public virtual void SetDefaultShape()
        {
            m_Modified = true;
        }
    }
}
