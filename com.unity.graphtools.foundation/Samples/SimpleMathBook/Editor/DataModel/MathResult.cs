using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Result")]
    public class MathResult : MathNode, IRebuildNodeOnConnection
    {
        public static TypeHandle[] DefaultAllowedInputs =
        {
            TypeHandle.Bool, TypeHandle.Float, TypeHandle.Int, TypeHandle.Vector2, TypeHandle.Vector3
        };

        public override TypeHandle[] ValueInputTypes => DefaultAllowedInputs;

        public override string Title
        {
            get => "Result";
            set {}
        }

        TypeHandle m_OperatorType = TypeHandle.Float;

        public override Value Evaluate()
        {
            var port = this.GetInputPorts().FirstOrDefault();

            return port.GetValue();
        }

        /// <inheritdoc />
        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(DataIn0);
            context.Statements.Add($"return {variable}");
            return variable;
        }

        public IPortModel DataIn0 { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            DataIn0 = this.AddDataInputPort("in", m_OperatorType);
        }

        public IEnumerable<IEdgeModel> RebuildOnEdgeConnected(IEdgeModel connectedEdge)
        {
            if (connectedEdge.ToPort.NodeModel == this && connectedEdge.FromPort.DataTypeHandle != m_OperatorType)
            {
                var edgeDiff = new NodeEdgeDiff(this, PortDirection.Input);

                m_OperatorType = connectedEdge.FromPort.DataTypeHandle;
                DefineNode();

                return edgeDiff.GetDeletedEdges();
            }

            return Enumerable.Empty<IEdgeModel>();
        }
    }
}
