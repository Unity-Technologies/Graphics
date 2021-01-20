namespace UnityEditor.ShaderGraph.Drawing
{
    interface ISGViewModel
    {
        GraphData Model { get; set; }

        // Wipes all data in this view-model
        void Reset();
    }
}
