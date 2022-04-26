using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathBook : GraphModel
    {
        [SerializeField]
        [Tooltip("The subgraph properties.")]
        SubgraphPropertiesField m_SubgraphPropertiesField = new SubgraphPropertiesField("A graph requires at least one input or output variable declaration to become usable as a subgraph.");
        public SubgraphPropertiesField SubgraphPropertiesField => m_SubgraphPropertiesField;

        public MathBookGraphProcessor EvaluationContext { get; set; }

        public override bool IsContainerGraph() => Asset is ContainerMathBookAsset;

        public MathBook()
        {
            StencilType = null;
        }

        public override Type DefaultStencilType => typeof(MathBookStencil);

        public override bool CanBeSubgraph() => VariableDeclarations.Any(variable => variable.IsInputOrOutput());

        public override ISubgraphNodeModel CreateSubgraphNode(IGraphModel referenceGraph, Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (referenceGraph.IsContainerGraph())
            {
                Debug.LogWarning("Failed to create the subgraph node. Container graphs cannot be referenced by a subgraph node.");
                return null;
            }
            return this.CreateNode<MathSubgraphNode>(referenceGraph.Name, position, guid, v => { v.SubgraphModel = referenceGraph; }, spawnFlags);
        }

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            var fromPort = startPortModel.Direction == PortDirection.Output ? startPortModel : compatiblePortModel;
            var toPort = startPortModel.Direction == PortDirection.Input ? startPortModel : compatiblePortModel;
            if (toPort.NodeModel is MathResult)
            {
                return fromPort.PortType == PortType.Data
                       && MathResult.DefaultAllowedInputs.Contains(fromPort.DataTypeHandle);
            }

            return base.IsCompatiblePort(startPortModel, compatiblePortModel);
        }
    }
}
