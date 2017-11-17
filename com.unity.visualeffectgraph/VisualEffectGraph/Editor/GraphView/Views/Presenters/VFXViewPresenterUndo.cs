using System;
using System.Linq;
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

            m_graphUndoCursor.hideFlags = HideFlags.HideAndDontSave;
            m_undoStack = new SortedDictionary<int, VFXGraph>();

            m_graphUndoCursor.index = 0;
            m_lastGraphUndoCursor = 0;
            m_undoStack.Add(0, initialState.Clone<VFXGraph>());
        }

        public void IncrementGraphState()
        {
            Undo.RecordObject(m_graphUndoCursor, string.Format("VFXGraph ({0})", m_graphUndoCursor.index + 1));
            m_graphUndoCursor.index = m_graphUndoCursor.index + 1;
        }

        public bool IsDirtyState()
        {
            return m_lastGraphUndoCursor != m_graphUndoCursor.index;
        }

        public void FlushAndPushGraphState(VFXGraph graph)
        {
            int lastCursorInStack = m_undoStack.Last().Key;
            while (lastCursorInStack > m_lastGraphUndoCursor)
            {
                //An action has been performed which overwrite
                m_undoStack.Remove(lastCursorInStack);
                lastCursorInStack = m_undoStack.Last().Key;
            }
            m_undoStack.Add(m_graphUndoCursor.index, graph.Clone<VFXGraph>());
        }

        public void CleanDirtyState()
        {
            m_lastGraphUndoCursor = m_graphUndoCursor.index;
        }

        public VFXGraph GetCopyCurrentGraphState()
        {
            VFXGraph refGraph = null;
            if (!m_undoStack.TryGetValue(m_graphUndoCursor.index, out refGraph))
            {
                throw new Exception(string.Format("Unable to retrieve current state at : {0} (max {1})", m_graphUndoCursor.index, m_undoStack.Last().Key));
            }
            return refGraph.Clone<VFXGraph>();
        }

        [NonSerialized]
        private SortedDictionary<int, VFXGraph> m_undoStack;
        [NonSerialized]
        private VFXGraphUndoCursor m_graphUndoCursor;
        [NonSerialized]
        private int m_lastGraphUndoCursor;
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

        private void ReleaseUndoStack()
        {
            m_graphUndoStack = null;
        }

        private void IncremenentGraphState()
        {
            if (!m_reentrant && m_graphUndoStack != null)
            {
                if (m_graphUndoStack == null)
                {
                    Debug.LogError("Unexpected IncrementGraphState (not initialize)");
                    return;
                }

                m_graphUndoStack.IncrementGraphState();
            }
        }

        public bool m_InLiveModification;

        public void BeginLiveModification()
        {
            if (m_InLiveModification == true)
                throw new InvalidOperationException("BeginLiveModification is not reentrant");
            m_InLiveModification = true;
        }

        public void EndLiveModification()
        {
            if (m_InLiveModification == false)
                throw new InvalidOperationException("EndLiveModification is not reentrant");
            m_InLiveModification = false;
            if (m_graphUndoStack.IsDirtyState())
            {
                m_graphUndoStack.FlushAndPushGraphState(m_Graph);
                m_graphUndoStack.CleanDirtyState();
            }
        }

        private void WillFlushUndoRecord()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            if (!m_InLiveModification)
            {
                if (m_graphUndoStack.IsDirtyState())
                {
                    m_graphUndoStack.FlushAndPushGraphState(m_Graph);
                    m_graphUndoStack.CleanDirtyState();
                }
            }
        }

        private void SynchronizeUndoRedoState()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            if (m_graphUndoStack.IsDirtyState())
            {
                try
                {
                    var cloneGraph = m_graphUndoStack.GetCopyCurrentGraphState();
                    m_VFXAsset.graph = cloneGraph;
                    m_reentrant = true;
                    ExpressionGraphDirty = true;
                    ForceReload();
                    m_reentrant = false;
                    m_graphUndoStack.CleanDirtyState();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Undo.ClearAll();
                    m_graphUndoStack = new VFXGraphUndoStack(m_Graph);
                }
            }
        }
    }
}
