using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    public class VFXGraphView : GraphView
    {
        public VFXGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
            : base(window, graphTool, graphViewName)
        {
            /*this.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            this.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

            this.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler);
            this.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler);*/
        }
    }
}
