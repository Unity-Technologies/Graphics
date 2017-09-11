using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXGraphUndoCursor : ScriptableObject
    {
        [SerializeField]
        public int index;
    }

    class VFXGraphUndoStack
    {
        public VFXGraphUndoStack(VFXGraph initialState)
        {
            m_graphUndoCursor = ScriptableObject.CreateInstance<VFXGraphUndoCursor>();
            m_graphUndoCursor.index = 0;
            CopyAndPushGraph(initialState);
        }

        private void CopyAndPushGraph(VFXGraph graph)
        {
            var clone = graph.Clone<VFXGraph>();
            m_undoStack.Add(clone);
        }

        public void PushGraphState(VFXGraph graph)
        {
            if (m_undoStack.Count - 1 != m_graphUndoCursor.index)
            {
                //An action has been performed after undo/redo
                m_undoStack = m_undoStack.GetRange(0, m_graphUndoCursor.index + 1);
            }

            CopyAndPushGraph(graph);
            Undo.RecordObject(m_graphUndoCursor, "VFXGraph");
            m_graphUndoCursor.index = m_undoStack.Count - 1;
        }

        public VFXGraph GetCopyCurrentGraphState()
        {
            if (m_graphUndoCursor.index > m_undoStack.Count)
                throw new Exception(string.Format("Unable to retrieve current state at : {0} (max {1})", m_graphUndoCursor.index, m_undoStack.Count));

            var refGraph = m_undoStack[m_graphUndoCursor.index];
            return refGraph.Clone<VFXGraph>();
        }

        [NonSerialized]
        private List<VFXGraph> m_undoStack = new List<VFXGraph>();
        [NonSerialized]
        private VFXGraphUndoCursor m_graphUndoCursor;
    }

    partial class VFXViewPresenter : GraphViewPresenter
    {
        [NonSerialized]
        private bool m_reentrant;
        [NonSerialized]
        private VFXGraphUndoStack m_graphUndoStack;

        private void InitializeUndoStack()
        {
            m_graphUndoStack = new VFXGraphUndoStack(m_Graph);
        }

        private void SynchronizeUndoRedoState()
        {
            var newAsset = new VFXAsset();
            newAsset.graph = m_graphUndoStack.GetCopyCurrentGraphState();
            m_reentrant = true;
            SetVFXAsset(newAsset, true);
            m_reentrant = false;
        }

        private void PushGraphState()
        {
            if (!m_reentrant)
            {
                m_reentrant = true;
                m_graphUndoStack.PushGraphState(m_Graph);
                m_reentrant = false;
            }
        }
    }
}
