namespace UnityEditor.ShaderGraph.Drawing
{
    interface ISGViewModel
    {
        GraphData Model { get; set; }

        void ConstructFromModel(GraphData graphData);
    }
}
