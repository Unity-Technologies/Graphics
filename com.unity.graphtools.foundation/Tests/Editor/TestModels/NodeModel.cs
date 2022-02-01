using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class NodeModel : BasicModel.NodeModel, IRenamable
    {
        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        public override bool AllowSelfConnect => true;

        public NodeModel()
        {
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        public override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
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
    }
}
