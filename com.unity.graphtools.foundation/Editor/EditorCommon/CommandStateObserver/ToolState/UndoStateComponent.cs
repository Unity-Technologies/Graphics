using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component to store undo information.
    /// </summary>
    public class UndoStateComponent : StateComponent<UndoStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for <see cref="UndoStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<UndoStateComponent>
        {
            static string GetUndoString(ICommand command)
            {
                if (command is UndoableCommand undoableCommand)
                {
                    return undoableCommand.UndoString ?? "";
                }

                return "";
            }

            /// <summary>
            /// Save a state component on the undo stack.
            /// </summary>
            /// <param name="stateComponent">The state component to save.</param>
            /// <param name="command">The command that is modifying the <paramref name="stateComponent"/>.</param>
            public void SaveSingleState(IUndoableStateComponent stateComponent, ICommand command)
            {
                // Used for cases in which multiple commands are dispatched within a single operation (eg.: SelectElementsCommand and MoveElementsCommand).
                Undo.IncrementCurrentGroup();

                var undoString = GetUndoString(command);

                var stateComponents = new IUndoableStateComponent[] { stateComponent, m_State.m_ToolStateComponent };
                foreach (var component in stateComponents)
                {
                    component.WillPerformUndoRedo(undoString);
                }
                m_State.m_UndoData.SetComponents(stateComponents);

                Undo.RegisterCompleteObjectUndo(new Object[] { m_State.m_UndoData }, undoString);
            }

            /// <summary>
            /// Save state components on the undo stack.
            /// </summary>
            /// <param name="stateComponents">The state components to save.</param>
            /// <param name="command">The command that is modifying the <paramref name="stateComponents"/>.</param>
            public void SaveStates(IUndoableStateComponent[] stateComponents, ICommand command)
            {
                var undoString = GetUndoString(command);

                var allStateComponents = stateComponents.Concat(new IUndoableStateComponent[] { m_State.m_ToolStateComponent });
                foreach (var component in allStateComponents)
                {
                    component.WillPerformUndoRedo(undoString);
                }
                m_State.m_UndoData.SetComponents(stateComponents);

                Undo.RegisterCompleteObjectUndo(new Object[] { m_State.m_UndoData }, undoString);
            }
        }

        IState m_State;
        UndoData m_UndoData;
        ToolStateComponent m_ToolStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoStateComponent"/> class.
        /// </summary>
        public UndoStateComponent(IState state, ToolStateComponent toolStateComponent)
        {
            m_State = state;
            m_UndoData = ScriptableObject.CreateInstance<UndoData>();
            m_UndoData.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            m_ToolStateComponent = toolStateComponent;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_UndoData = null;
            }
        }

        /// <summary>
        /// Apply the data that was saved on the undo state to the state components, replacing data they were previously holding.
        /// </summary>
        public void ApplyUndoData()
        {
            m_UndoData.ApplyUndoDataToState(m_State);

            foreach (var component in m_State.AllStateComponents.OfType<IUndoableStateComponent>())
            {
                component.UndoRedoPerformed();
            }
        }
    }
}
