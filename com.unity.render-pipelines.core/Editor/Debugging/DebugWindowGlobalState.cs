using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    sealed class DebugWindowGlobalState : ScriptableSingleton<DebugWindowGlobalState>
    {
        public int selectedPanel;

        [SerializeField]
        private SerializedDictionary<string, DebugState> m_WidgetStates;

        public SerializedDictionary<string, DebugState> states
        {
            get
            {
                // States are ScriptableObjects (necessary for Undo/Redo)
                if (m_WidgetStates == null)
                    m_WidgetStates = new();

                // If any internal reference is destroyed, destroy everything to reinit the states
                if (m_WidgetStates.Any(state => state.Value == null))
                    DestroyWidgetStates();

                return m_WidgetStates;
            }
        }

        /// <summary>
        /// Destroys the widget states if the <see cref="force"/> is set to true or if it is false but the state hash has changed
        /// </summary>
        /// <param name="force">If the state hash should not be taken into account</param>
        public void DestroyWidgetStates()
        {
            if (m_WidgetStates == null)
                return;

            // Clear all the states from memory
            foreach (var s in m_WidgetStates.Select(state => state.Value))
            {
                Undo.ClearUndo(s); // Don't leave dangling states in the global undo/redo stack
                DestroyImmediate(s);
            }

            m_WidgetStates.Clear();
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave; // Needed for exiting playmode, tells the AssetDatabase to not release the object
        }

        // We use item states to keep a cached value of each serializable debug items in order to
        // handle domain reloads, play mode entering/exiting and undo/redo
        // Note: no removal of orphan states
        public void UpdateWidgetStates()
        {
            // We store the updates to not raise possible onValueChangedEvents, that possibly will change the collection
            // of widgets from the panels
            m_WidgetsToUpdate.Clear();

            foreach (var panel in DebugManager.instance.panels)
                UpdateWidgetStates(panel);

            foreach (var (valueField, state) in m_WidgetsToUpdate)
                valueField.SetValue(state.GetValue());
        }

        List<(DebugUI.IValueField valueField, DebugState state)> m_WidgetsToUpdate = new();

        void UpdateWidgetStates(DebugUI.IContainer container)
        {
            // Skip runtime only containers, we won't draw them so no need to serialize them either
            if (container is DebugUI.Widget actualWidget && actualWidget.isInactiveInEditor)
                return;

            var stateFactory = DebugUIWidgetToDebugStateFactory.instance;

            // Recursively update widget states
            foreach (var widget in container.children)
            {
                // Skip non-serializable widgets but still traverse them in case one of their
                // children needs serialization support
                if (widget is DebugUI.IValueField valueField)
                {
                    // Skip runtime & readonly only items
                    if (widget.isInactiveInEditor)
                        return;

                    string guid = widget.queryPath;
                    if (!states.TryGetValue(guid, out var state) || state == null)
                    {
                        // Create missing states & recreate the ones that are null
                        if (stateFactory.CreateDebugState(widget.GetType(), out var inst))
                        {
                            inst.queryPath = guid;
                            inst.SetValue(valueField.GetValue(), valueField);
                            states[guid] = inst;
                        }
                    }
                    else
                    {
                        if (valueField.GetValue() != state.GetValue())
                            m_WidgetsToUpdate.Add((valueField, state));
                    }
                }

                // Recurse if the widget is a container
                if (widget is DebugUI.IContainer containerField)
                    UpdateWidgetStates(containerField);
            }
        }

        public void ApplyStates(bool forceApplyAll = false)
        {
            if (!forceApplyAll && DebugState.m_CurrentDirtyState != null)
            {
                ApplyState(DebugState.m_CurrentDirtyState.queryPath, DebugState.m_CurrentDirtyState);
                DebugState.m_CurrentDirtyState = null;
                return;
            }

            foreach (var state in DebugWindowGlobalState.instance.states)
                ApplyState(state.Key, state.Value);

            DebugState.m_CurrentDirtyState = null;
        }

        void ApplyState(string queryPath, DebugState state)
        {
            if (DebugManager.instance.GetItem(queryPath) is not DebugUI.IValueField widget)
                return;

            widget.SetValue(state.GetValue());
        }

        #region StateHash API

        public int currentStateHash;

        [SerializeField] private SerializedDictionary<string, int> m_StateHashes = new();

        public int ComputeStateHash()
        {
            unchecked
            {
                unchecked
                {
                    int hash = 13;

                    foreach (var state in m_WidgetStates)
                    {
                        var stateHash = 23 + state.Value.GetHashCode();

                        if (m_StateHashes.TryGetValue(state.Key, out var storedHash))
                        {
                            if (storedHash != stateHash)
                                Debug.Log($"{state.Key} has changed it hash");
                        }

                        m_StateHashes[state.Key] = stateHash;
                        hash *= stateHash;
                    }

                    return hash;
                }
            }
        }

        public void UpdateStateHash()
        {
            currentStateHash = ComputeStateHash();
        }

        #endregion
    }
}
