namespace UnityEditor.ShaderGraph
{
    interface IGraphDataAction
    {
        // Takes in GraphData, performs some modification on it, and then returns it
        GraphData MutateGraphData(GraphData initialState);
    }
}
