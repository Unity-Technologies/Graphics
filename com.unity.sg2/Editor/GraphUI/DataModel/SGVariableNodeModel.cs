using System;
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
