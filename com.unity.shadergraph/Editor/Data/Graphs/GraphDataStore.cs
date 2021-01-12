using UnityEditor.ShaderGraph;
using ActionType = UnityEditor.ShaderGraph.IGraphDataAction;
using System;

namespace UnityEditor.ShaderGraph
{
    delegate T Reducer<T> (T state, ActionType action);

    class DataStore<T>
    {
        Reducer<T> m_Reducer;
        public T State { get; private set; }

        public Action<T> Subscribe;

        public DataStore(Reducer<T> reducer, T initialState)
        {
            m_Reducer = reducer;
            State = initialState;
        }

        public void Dispatch(ActionType action)
        {
            State = m_Reducer(State, action);
            // Notifies any listeners about change in state
            Subscribe?.Invoke(State);
        }
    }
}
