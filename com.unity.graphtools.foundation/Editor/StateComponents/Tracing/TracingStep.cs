namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A frame is a collection of Tracing Steps. Each step is either a node being executed, a trigger port being fired,
    /// an input data port being read or an output data port being written
    /// </summary>
    public struct TracingStep
    {
        public TracingStepType Type;
        public INodeModel NodeModel;
        public IPortModel PortModel;

        public byte Progress;

        public string ErrorMessage;
        public string ValueString;

        public static TracingStep ExecutedNode(INodeModel nodeModel1, byte progress) =>
            new TracingStep
        {
            Type = TracingStepType.ExecutedNode,
            NodeModel = nodeModel1,
            Progress = progress,
        };

        public static TracingStep TriggeredPort(IPortModel portModel) =>
            new TracingStep
        {
            Type = TracingStepType.TriggeredPort,
            NodeModel = portModel.NodeModel,
            PortModel = portModel,
        };

        public static TracingStep WrittenValue(IPortModel portModel, string valueString) =>
            new TracingStep
        {
            Type = TracingStepType.WrittenValue,
            NodeModel = portModel.NodeModel,
            PortModel = portModel,
            ValueString = valueString,
        };

        public static TracingStep ReadValue(IPortModel portModel, string valueString) =>
            new TracingStep
        {
            Type = TracingStepType.ReadValue,
            NodeModel = portModel.NodeModel,
            PortModel = portModel,
            ValueString = valueString,
        };

        public static TracingStep Error(INodeModel nodeModel, string error) =>
            new TracingStep
        {
            Type = TracingStepType.Error,
            NodeModel = nodeModel,
            ErrorMessage = error,
        };
    }
}
