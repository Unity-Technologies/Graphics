using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class used to serialize and deserialize state components for undo operations.
    /// </summary>
    [Serializable]
    public class UndoData : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<string> m_StateComponentTypeNames;

        [SerializeField]
        List<string> m_SerializedState;

        List<IUndoableStateComponent> m_StateComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoData"/> class.
        /// </summary>
        public UndoData()
        {
            m_StateComponentTypeNames = new List<string>();
            m_SerializedState = new List<string>();
            m_StateComponents = new List<IUndoableStateComponent>();
        }

        /// <summary>
        /// Sets the state component as the single state component held by this object.
        /// </summary>
        /// <param name="stateComponent">The state component.</param>
        public void SetSingleComponent(IUndoableStateComponent stateComponent)
        {
            m_StateComponents.Clear();
            m_StateComponents.Add(stateComponent);
        }

        /// <summary>
        /// Sets the state components as the state components held by this object.
        /// </summary>
        /// <param name="stateComponents">The state components.</param>
        public void SetComponents(IUndoableStateComponent[] stateComponents)
        {
            m_StateComponents.Clear();
            m_StateComponents.AddRange(stateComponents);
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            m_SerializedState.Clear();
            m_StateComponentTypeNames.Clear();
            foreach (var stateComponent in m_StateComponents)
            {
                var fullTypeName = stateComponent.GetType().AssemblyQualifiedName;
                m_StateComponentTypeNames.Add(fullTypeName);

                var serializedState = StateComponentHelper.Serialize(stateComponent);
                m_SerializedState.Add(serializedState);
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// Update all state components with the data serialized.
        /// </summary>
        /// <param name="state">The state that holds the state components.</param>
        public void ApplyUndoDataToState(IState state)
        {
            for (var i = 0; i < m_SerializedState.Count; i++)
            {
                var fullTypeName = m_StateComponentTypeNames[i];
                var type = Type.GetType(fullTypeName);

                if (StateComponentHelper.Deserialize(m_SerializedState[i], type) is IUndoableStateComponent newStateComponent)
                {
                    var stateComponent = state.AllStateComponents.OfType<IUndoableStateComponent>().FirstOrDefault(c => c.Guid == newStateComponent.Guid);
                    stateComponent?.Apply(newStateComponent);
                }
            }
        }
    }
}
