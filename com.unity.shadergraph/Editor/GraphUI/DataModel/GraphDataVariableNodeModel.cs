using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableNodeModel : VariableNodeModel, IGraphDataPortModelOwner
    {
        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// Graph data name associated with this node. If null, this node is a searcher preview with type determined
        /// by the registryKey property.
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        /// <summary>
        /// This node's registry key. If graphDataName is set, this is read from the graph. Otherwise, it is set
        /// manually using SetPreviewRegistryKey.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                Assert.IsTrue(TryGetNodeReader(out var reader));
                return reader.GetRegistryKey();
            }
        }

        /// <summary>
        /// Determines whether or not this node has a valid backing representation at the data layer. If false, this
        /// node should be treated as a searcher preview.
        /// </summary>
        public bool existsInGraphData => m_GraphDataName != null && TryGetNodeReader(out _);

        GraphHandler graphHandler => ((ShaderGraphModel)GraphModel).GraphHandler;

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

        protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new GraphDataPortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                AssetModel = AssetModel
            };
        }
    }
}
