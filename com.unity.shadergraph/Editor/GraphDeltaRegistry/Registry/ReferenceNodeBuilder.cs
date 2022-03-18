using System;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ReferenceNodeBuilder : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new() { Name = "Reference", Version = 1 };

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            // TODO: Correctly generate port type based on our reference type (how do we find that?).
        }

        public ShaderFunction GetShaderFunction(INodeReader data, ShaderContainer container, Registry registry)
        {
            // Reference nodes are not processed through function generation.
            throw new NotImplementedException();
        }
    }
}
