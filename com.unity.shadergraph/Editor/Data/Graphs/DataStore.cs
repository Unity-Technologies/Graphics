using UnityEditor.ShaderGraph;
using ActionType = UnityEditor.ShaderGraph.IGraphDataAction;
using System;

namespace UnityEditor.ShaderGraph
{
    delegate T Reducer<T> (T state, ActionType action);

    class DataStore<T>
    {
        Reducer<T> m_Reducer;
        internal T State { get; private set; }

        internal Action<T> Subscribe;

        internal DataStore(Reducer<T> reducer, T initialState)
        {
            m_Reducer = reducer;
            State = initialState;
        }

        internal void Dispatch(ActionType action)
        {
            State = m_Reducer(State, action);
            // Notifies any listeners about change in state
            Subscribe?.Invoke(State);
        }
    }
}
