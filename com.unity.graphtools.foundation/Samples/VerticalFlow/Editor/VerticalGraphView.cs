using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphView : GraphView
    {
        public VerticalGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
            : base(window, graphTool, graphViewName)
        {
            SetupZoom(0.05f, 5.0f, 5.0f);

            this.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            this.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);
        }
    }
}
