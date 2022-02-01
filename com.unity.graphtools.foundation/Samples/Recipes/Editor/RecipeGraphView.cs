using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphView : GraphView
    {
        public RecipeGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
            : base(window, graphTool, graphViewName)
        {
            this.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            this.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

            this.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler);
            this.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler);
        }
    }
}
