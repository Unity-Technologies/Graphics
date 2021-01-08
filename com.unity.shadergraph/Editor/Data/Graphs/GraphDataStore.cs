using UnityEditor.ShaderGraph;
using ActionType = UnityEditor.ShaderGraph.IGraphDataAction;

namespace UnityEditor.Graphing
{
    delegate T Reducer<T> (T state, ActionType action);

    class DataStore<T>
    {
        Reducer<T> m_Reducer;
        public T State { get; private set; }

        public DataStore(Reducer<T> reducer, T initialState)
        {
            m_Reducer = reducer;
            State = initialState;
        }

        public void Dispatch(ActionType action)
        {
            State = m_Reducer(State, action);
        }
    }
}
