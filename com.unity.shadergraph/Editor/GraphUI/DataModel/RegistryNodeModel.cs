using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Exploration;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// A RegistryNodeModel is a NodeModel whose topology is determined by looking up a key in the ShaderGraph
    /// Registry.
    /// </summary>
    public class RegistryNodeModel : NodeModel
    {
        [SerializeField]
        RegistryKey m_RegistryKey;

        /// <summary>
        /// The registry key used to look up this node's topology. Must be set before DefineNode is called.
        ///
        /// RegistryNodeSearcherItem sets this in an initialization callback, and the extension method
        /// GraphModel.CreateRegistryNode also handles assigning it.
        /// </summary>
        public RegistryKey registryKey
        {
            get => m_RegistryKey;
            set => m_RegistryKey = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            var stencil = (ShaderGraphStencil) GraphModel.Stencil;
            var registry = stencil.GetRegistry();
            var reader = registry.GetDefaultTopology(registryKey);

            if (reader != null)
            {
                AddPortFromReader(reader, "out");
            }
        }

        void AddPortFromReader(GraphDelta.INodeReader reader, string name)
        {
            if (!reader.TryGetPort(name, out var portReader)) return;



            var isInput = portReader.GetFlags().isInput;
            var orientation = portReader.GetFlags().isHorizontal
                ? PortOrientation.Horizontal
                : PortOrientation.Vertical;

            var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetRegistryKey());

            if (isInput)
                this.AddDataInputPort(name, type, orientation: orientation);
            else
                this.AddDataOutputPort(name, type, orientation: orientation);
        }
    }
}
