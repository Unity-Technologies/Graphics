namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphView : GraphView
    {
        public VerticalGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
            SetupZoom(0.05f, 5.0f, 5.0f);

            if (displayMode == GraphViewDisplayMode.Interactive)
            {
                this.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
                this.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);
            }
        }
    }
}
