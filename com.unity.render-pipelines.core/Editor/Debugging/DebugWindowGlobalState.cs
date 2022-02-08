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

        #region StateHash API

        public int currentStateHash;

        public int ComputeStateHash()
        {
            unchecked
            {
                return states.Aggregate(13, (current, state) => current * 23 + state.Value.GetHashCode());
            }
        }

        public void UpdateStateHas()
        {
            currentStateHash = ComputeStateHash();
        }

        #endregion
    }
}
