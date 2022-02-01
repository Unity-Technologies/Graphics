using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathResult : NodeModel, IRebuildNodeOnConnection
    {
        public override string Title
        {
            get => "Result";
            set {}
        }

        private TypeHandle _operatorType = TypeHandle.Float;

        public Value Evaluate()
        {
            var port = this.GetInputPorts().FirstOrDefault();

            return port.GetValue();
        }

        public IPortModel DataIn0 { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            DataIn0 = this.AddDataInputPort("in", _operatorType, options: PortModelOptions.NoEmbeddedConstant);
        }

        public bool RebuildOnEdgeConnected(IEdgeModel connectedEdge)
        {
            if (connectedEdge.ToPort.NodeModel == this && connectedEdge.FromPort.DataTypeHandle != _operatorType)
            {
                _operatorType = connectedEdge.FromPort.DataTypeHandle;
                DefineNode();
                return true;
            }

            return false;
        }
    }
}
