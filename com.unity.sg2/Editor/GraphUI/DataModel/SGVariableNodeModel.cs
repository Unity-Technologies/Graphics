using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.GraphToolsFoundation.Editor;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Represents an instance of a blackboard property/keyword on the graph
    /// </summary>
    class SGVariableNodeModel : VariableNodeModel, IGraphDataOwner
    {
        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// Graph data name associated with this node.
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        /// <summary>
        /// This node's registry key. If graphDataName is set, this is read from the graph.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                Assert.IsTrue(TryGetNodeReader(out var reader));
                return reader.GetRegistryKey();
            }
        }

        public SGPortModel outputPortModel => (SGPortModel)m_MainPortModel;

        /// <summary>
        /// Determines whether or not this node has a valid backing representation at the data layer.
        /// </summary>
        public bool existsInGraphData => m_GraphDataName != null && TryGetNodeReader(out _);

        GraphHandler graphHandler => ((SGGraphModel)GraphModel).GraphHandler;

        protected override void OnDefineNode()
        {
            m_MainPortModel = this.AddDataOutputPort(null, this.GetDataType(), ReferenceNodeBuilder.kOutput);
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            if (DeclarationModel is SGVariableDeclarationModel declarationModel)
            {
                // if the blackboard property/keyword this variable node is referencing
                // doesn't exist in the graph, it has probably been copied from another graph
                if (!GraphModel.VariableDeclarations.Contains(declarationModel))
                {
                    // Search for the equivalent property/keyword that GTF code
                    // will have created to replace the missing reference
                    DeclarationModel = GraphModel.VariableDeclarations.FirstOrDefault(model => model.Guid == declarationModel.Guid);

                    // Restore the Guid from its graph data name (as currently we need to align the Guids and graph data names)
                    graphDataName = new SerializableGUID(graphDataName.Replace("_", String.Empty)).ToString();

                    // Make sure this reference is up to date
                    declarationModel = (SGVariableDeclarationModel)DeclarationModel;
                }
                else
                    graphDataName = Guid.ToString();

                // Every time a variable node is duplicated, add a reference node pointing back
                // to the property/keyword that is wrapped by the VariableDeclarationModel, on the CLDS level
                (GraphModel as SGGraphModel).GraphHandler.AddReferenceNode(graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
            }

            base.OnDuplicateNode(sourceNode);
        }

        public bool TryGetNodeReader(out NodeHandler reader)
        {
            try
            {
                reader = graphHandler.GetNode(graphDataName);
                return reader != null;
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed to retrieve node due to exception:" + exception);
                reader = null;
                return false;
            }
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options);
        }
    }
}
