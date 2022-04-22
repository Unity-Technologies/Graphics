using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class EdgeModel : BasicModel.EdgeModel
    {
        IPortModel m_FromPort;
        IPortModel m_ToPort;

        public override string EdgeLabel => m_EdgeLabel;

        public override IPortModel FromPort
        {
            get => m_FromPort;
            set
            {
                OnPortChanged(m_FromPort, value);
                m_FromPort = value;
            }
        }

        public override IPortModel ToPort
        {
            get => m_ToPort;
            set
            {
                OnPortChanged(m_ToPort, value);
                m_ToPort = value;
            }
        }

        public override string FromPortId => FromPort.UniqueName;

        public override string ToPortId => ToPort.UniqueName;

        public override SerializableGUID FromNodeGuid => FromPort.NodeModel.Guid;

        public override SerializableGUID ToNodeGuid => ToPort.NodeModel.Guid;
    }
}
