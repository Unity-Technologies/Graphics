using ActionType = UnityEditor.ShaderGraph.IGraphDataAction;

namespace UnityEditor.ShaderGraph
{
    delegate T Reducer<T> (T state, ActionType action);

    class GraphDataStore
    {
        Reducer<GraphData> m_Reducer;
        public GraphData State { get; private set; }

        public GraphDataStore(Reducer<GraphData> reducer, GraphData initialState)
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
