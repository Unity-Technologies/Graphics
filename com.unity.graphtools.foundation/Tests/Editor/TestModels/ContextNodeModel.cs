using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class ContextNodeModel : BasicModel.ContextNodeModel
    {
        public override bool AllowSelfConnect => true;

        protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            var port = new PortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName,
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                AssetModel = AssetModel
            };
            return port;
        }

        public int ExeInputCount { get; set; }
        public int ExeOuputCount { get; set; }

        public int InputCount { get; set; }
        public int OuputCount { get; set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            for (var i = 0; i < ExeInputCount; i++)
                this.AddExecutionInputPort("Exe In " + i);

            for (var i = 0; i < ExeOuputCount; i++)
                this.AddExecutionOutputPort("Exe Out " + i);

            for (var i = 0; i < InputCount; i++)
                this.AddDataInputPort("In " + i, TypeHandle.Unknown);

            for (var i = 0; i < OuputCount; i++)
                this.AddDataOutputPort("Out " + i, TypeHandle.Unknown);
        }
    }
}
