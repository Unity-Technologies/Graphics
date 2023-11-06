using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    [Serializable]
    class VFXGraphUndoStack
    {
        public enum RestoreResult
        {
            None,
            FullGraph,
            Deltas,
        };

        public VFXGraphUndoStack(VFXGraph initialState)
        {
            m_graphUndoCursor = ScriptableObject.CreateInstance<VFXGraphUndoCursor>();

            m_graphUndoCursor.hideFlags = HideFlags.HideAndDontSave;
            m_undoStack = new List<IBackupState>();

            m_CurrentDeltas = new BackupDeltas();

            m_graphUndoCursor.index = 0;
            m_undoStack.Add(new BackupGraph() { graphData = initialState.Backup() });
            m_Graph = initialState;

            m_NeedsFlush = false;
            m_CurrentCursor = 0;
        }

        public void UpdateState(VFXModel model, VFXModel.InvalidationCause cause)
        {
            bool newGroup = m_CurrentUndoGroup != Undo.GetCurrentGroup();
            if (newGroup)
            {
                m_CurrentDeltas = new BackupDeltas();
                m_FullGraphBackup = false;
            }

            bool stateUpdated = false;
            switch (cause)
            {
                case VFXModel.InvalidationCause.kParamChanged:
                    {
                        if (model is VFXSlot slot) // model not beeing a VFXSlot means it is a subgraph reporting a value change
                        {
                            AddSlotValueChange(slot);
                            stateUpdated = true;
                        }
                        else if (model is VFXParameter) // Cannot use fast path for VFX parameters
                        {
                            m_FullGraphBackup = true;
                            stateUpdated = true;
                        }
                    }
                    break;

                case VFXModel.InvalidationCause.kUIChanged:
                    {
                        // If model is null or graph (meaning group or stickynote change) or is a VFX parameter, we cannot use fast path and have to serialize all graph
                        if (model == null || model is VFXGraph || model is VFXParameter)
                        { 
                            m_FullGraphBackup = true;
                        }
                        else // fast path for model UI change
                        {
                            AddModelUIChange(model);
                        }
                        stateUpdated = true;
                    }
                    break;

                case VFXModel.InvalidationCause.kStructureChanged:
                case VFXModel.InvalidationCause.kSettingChanged:
                case VFXModel.InvalidationCause.kSpaceChanged:
                case VFXModel.InvalidationCause.kConnectionChanged:
                    {
                        m_FullGraphBackup = true;
                        stateUpdated = true;
                    }
                    break;
            }

            if (!stateUpdated)
                return;

            if (newGroup)
            {
                Undo.RecordObject(m_graphUndoCursor, string.Format("Modify VFX Graph - {0} ({1})", m_Graph?.GetResource()?.name, m_graphUndoCursor.index + 1));
                m_graphUndoCursor.index = m_graphUndoCursor.index + 1;
                m_CurrentUndoGroup = Undo.GetCurrentGroup();
                m_CurrentCursor = m_graphUndoCursor.index;
            }

            m_NeedsFlush = true;
        }

        public void FlushState()
        {
            if (!m_NeedsFlush)
                return;

            int entriesCount = m_undoStack.Count();
            if (entriesCount != m_CurrentCursor + 1)
            {
                if (entriesCount == m_CurrentCursor)
                    m_undoStack.Add(null);
                else if (entriesCount > m_CurrentCursor + 1)
                    m_undoStack.RemoveRange(m_CurrentCursor + 1, m_undoStack.Count() - (m_CurrentCursor + 1));
                else
                    throw new InvalidOperationException("Corrupted VFX Graph undo stack - Missing entries");
            }

            // Store state
            if (m_FullGraphBackup)
            {
                m_undoStack[m_CurrentCursor] = new BackupGraph() { graphData = m_Graph.Backup() };
            }
            else
            {
                m_undoStack[m_CurrentCursor] = new BackupDeltas()
                {
                    slotValues = m_CurrentDeltas.slotValues,
                    modelUI = m_CurrentDeltas.modelUI,
                };
            }

            m_NeedsFlush = false;
        }

        public RestoreResult RestoreState()
        {
            if (m_CurrentCursor == m_graphUndoCursor.index)
                return RestoreResult.None;

            int order = Math.Sign(m_CurrentCursor - m_graphUndoCursor.index);
            bool needsRecompile = false;
            do
            {
                if (order == 1) // undo
                {
                    // Undoing a full graph backup, needs to go back to previous backup and replay delta changes 
                    if (m_undoStack[m_CurrentCursor] is BackupGraph)
                    {
                        int currentRestoredCursor = m_graphUndoCursor.index;
                        while (!(m_undoStack[currentRestoredCursor] is BackupGraph))
                            --currentRestoredCursor;

                        while (currentRestoredCursor <= m_graphUndoCursor.index)
                        {
                            m_undoStack[currentRestoredCursor].Apply(m_Graph, true);
                            ++currentRestoredCursor;
                        }

                        needsRecompile = true;
                    }
                    // Undoing a delta command, simply send delta notifications 
                    else
                    {
                        m_undoStack[m_CurrentCursor].Apply(m_Graph, false);
                    }
                }
                else // order == -1 // redo
                {
                    needsRecompile = m_undoStack[m_graphUndoCursor.index].Apply(m_Graph, false);
                }

                m_CurrentCursor -= order;
            }
            while (m_CurrentCursor != m_graphUndoCursor.index);

            return needsRecompile ? RestoreResult.FullGraph : RestoreResult.Deltas;
        }

        public void AddSlotValueChange(VFXSlot slot)
        {
            if (slot != null)
            {
                if (m_CurrentDeltas.slotValues == null)
                    m_CurrentDeltas.slotValues = new Dictionary<VFXSlot, object>();
                m_CurrentDeltas.slotValues[slot] = slot.value;
            }
        }

        public void AddModelUIChange(VFXModel model) 
        {
            if (model != null)
            {
                if (m_CurrentDeltas.modelUI == null)
                    m_CurrentDeltas.modelUI = new Dictionary<VFXModel, UIState>();
                m_CurrentDeltas.modelUI[model] = new UIState
                {
                    pos = model.position,
                    collapsed = model.collapsed,
                    superCollapsed = model.superCollapsed
                };
            }
        }

        struct UIState
        {
            public Vector2 pos;
            public bool collapsed;
            public bool superCollapsed;
        }

        interface IBackupState
        {
            // Apply state to graph and returns true if recompilation is needed, false otherwise
            public bool Apply(VFXGraph graph, bool updateDeltas);
        }

        class BackupGraph : IBackupState
        {
            public object graphData;

            public bool Apply(VFXGraph graph, bool updateDeltas)
            {
                graph.Restore(graphData);
                return true;
            }
        }

        class BackupDeltas : IBackupState
        {
            public Dictionary<VFXSlot, object> slotValues;
            public Dictionary<VFXModel, UIState> modelUI;

            public bool Apply(VFXGraph graph, bool updateDeltas)
            {
                if (slotValues != null)
                    foreach (var kv in slotValues)
                    {
                        kv.Key.value = updateDeltas ? kv.Value : kv.Key.value;
                    }

                if (modelUI != null)
                    foreach (var kv in modelUI)
                    {
                        if (updateDeltas)
                        {
                            kv.Key.position = kv.Value.pos;
                            kv.Key.collapsed = kv.Value.collapsed;
                            kv.Key.superCollapsed = kv.Value.superCollapsed;
                        }
                        else
                        {
                            kv.Key.Invalidate(VFXModel.InvalidationCause.kUIChanged);
                        }
                    }

                return false;  
            } 
        }

        [SerializeField]
        private VFXGraph m_Graph;
        [SerializeField]
        private int m_CurrentCursor;
        [SerializeField]
        private List<IBackupState> m_undoStack;
        [SerializeField]
        private VFXGraphUndoCursor m_graphUndoCursor;

        private BackupDeltas m_CurrentDeltas;
        private bool m_NeedsFlush;
        private int m_CurrentUndoGroup = -1;
        private bool m_FullGraphBackup = false;
    }

    partial class VFXViewController : Controller<VisualEffectResource>
    {
        [NonSerialized]
        private bool m_reentrantUndo;
        [SerializeField]
        private VFXGraphUndoStack m_graphUndoStack;

        public bool isReentrant => m_reentrantUndo;

        private void InitializeUndoStack()
        {
            m_graphUndoStack = new VFXGraphUndoStack(graph);
        }

        private void ReleaseUndoStack()
        {
            m_graphUndoStack = null; 
        }

        public void IncremenentGraphUndoRedoState(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (m_graphUndoStack == null || m_reentrantUndo)
                return;

            m_graphUndoStack.UpdateState(model, cause);
        }

        private void WillFlushUndoRecord()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            m_graphUndoStack.FlushState();
        }

        private void SynchronizeUndoRedoState()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            m_reentrantUndo = true;
            try
            {
                var result = m_graphUndoStack.RestoreState();

                if (result != VFXGraphUndoStack.RestoreResult.None)
                {
                    if (result == VFXGraphUndoStack.RestoreResult.FullGraph)
                    {
                        ExpressionGraphDirty = true;
                        model.GetOrCreateGraph().UpdateSubAssets();
                        EditorUtility.SetDirty(graph);
                        NotifyUpdate();
                    }
                    else // deltas
                    {
                        ExpressionGraphDirty = true;
                        ExpressionGraphDirtyParamOnly = true;
                        graph.SetExpressionValueDirty();
                    }
                }
            }
            finally
            {
                m_reentrantUndo = false;
            }
        }
    }
}
