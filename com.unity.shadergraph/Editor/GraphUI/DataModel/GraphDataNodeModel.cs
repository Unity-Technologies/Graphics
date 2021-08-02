using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class GraphDataNodeModel : NodeModel
    {
        string m_Name;

        public string graphDataName
        {
            get => m_Name ??= Guid.ToString();
            set => m_Name = value;
        }

        IGraphHandler graphHandler => ((ShaderGraphModel) GraphModel).GraphHandler;

        public INodeWriter nodeWriter => graphHandler.GetNodeWriter(m_Name);
        public INodeReader nodeReader => graphHandler.GetNode(m_Name);
        public RegistryKey registryKey => nodeReader.GetRegistryKey();

        public GraphDataNodeModel()
        {
            m_PreviousInputs ??= new OrderedPorts();
            m_PreviousOutputs ??= new OrderedPorts();
        }

        protected override void OnDefineNode()
        {
            INodeReader reader;

            // TODO: Non-final error handling
            try
            {
                reader = nodeReader;
            }
            catch (NullReferenceException e)
            {
                Debug.LogError($"Node {graphDataName} is missing in graph data");
                Debug.LogException(e);
                return;
            }

            foreach (var portReader in reader.GetPorts())
            {
                var isInput = portReader.GetFlags().isInput;
                var orientation = portReader.GetFlags().isHorizontal
                    ? PortOrientation.Horizontal
                    : PortOrientation.Vertical;

                var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetRegistryKey());

                if (isInput)
                    this.AddDataInputPort(portReader.GetName(), type, orientation: orientation);
                else
                    this.AddDataOutputPort(portReader.GetName(), type, orientation: orientation);
            }
        }

        public override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
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
