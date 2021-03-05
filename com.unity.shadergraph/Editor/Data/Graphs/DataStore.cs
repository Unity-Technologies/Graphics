using UnityEditor.ShaderGraph;
using ActionType = UnityEditor.ShaderGraph.IGraphDataAction;
using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class DataStore<T>
    {
        Action<T, ActionType> m_Reducer;
        internal T State { get; private set; }

        public Action<T, ActionType> Subscribe;

        internal DataStore(Action<T, ActionType> reducer, T initialState)
        {
            m_Reducer = reducer;
            State = initialState;
        }

        public void Dispatch(ActionType action)
        {
            try
            {
                m_Reducer(State, action);
            }
            catch (Exception exception)
            {
                Debug.Log("Ran into exception of type: " + exception + " while dispatching action of type: " + action + "to DataStore.");
                return;
            }

            // Note: This would only work with reference types, as value types would require creating a new copy, this works given that we use GraphData which is a heap object
            // Notifies any listeners about change in state
            try
            {
                Subscribe?.Invoke(State, action);
            }
            catch (Exception exception)
            {
                Debug.Log("Ran into exception of type: " + exception + " while notifying graph objects about action of type: " + action);
            }
        }
    }
}
