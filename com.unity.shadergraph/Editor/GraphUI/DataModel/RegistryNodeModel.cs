using System;
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
        public PlaceholderRegistryKey key { get; set; }  // FIXME

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            // TODO: Build node topology from registry definition
        }
    }
}
