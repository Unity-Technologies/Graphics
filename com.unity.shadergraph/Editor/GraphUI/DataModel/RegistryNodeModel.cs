using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// A RegistryNodeModel is a NodeModel whose topology is determined by looking up a key in the ShaderGraph
    /// Registry.
    /// </summary>
    public class RegistryNodeModel : NodeModel
    {
        [SerializeField]
        PlaceholderRegistryKey m_RegistryKey;

        /// <summary>
        /// The registry key used to look up this node's topology. Must be set before DefineNode is called.
        ///
        /// RegistryNodeSearcherItem sets this in an initialization callback, and the extension method
        /// GraphModel.CreateRegistryNode also handles assigning it.
        /// </summary>
        public PlaceholderRegistryKey registryKey
        {
            get => m_RegistryKey;
            set => m_RegistryKey = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            // TODO: Build node topology from registry definition
        }
    }
}
