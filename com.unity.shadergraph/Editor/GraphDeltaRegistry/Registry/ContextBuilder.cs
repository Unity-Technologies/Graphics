using System;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ContextBuilder : INodeDefinitionBuilder
    {
        public static readonly RegistryKey kRegistryKey = new() { Name = "_Context", Version = 1 };

        public RegistryKey GetRegistryKey() =>
            kRegistryKey;

        public RegistryFlags GetRegistryFlags() =>
            RegistryFlags.Base;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var contextKey = node.GetMetadata<RegistryKey>("_contextDescriptor");
            var context = registry.GetContextDescriptor(contextKey);
            foreach (var entry in context.GetEntries())
            {
                var port = node.AddPort<GraphType>(entry.fieldName, true, registry);
                port.GetTypeField().AddSubField(GraphType.kEntry, entry);
                for (int i = 0; i < (int)entry.length * (int)entry.height; ++i)
                    port.GetTypeField().GetSubField<float>($"c{i}").SetData(entry.initialValue[i]);
                if (entry.interpolationSemantic == null || entry.interpolationSemantic == "")
                    port.GetTypeField().AddSubField("semantic", entry.interpolationSemantic);
            }
            // We could "enfield" all of our ContextEntries into our output port,
            // which would consequently make them accessible with regards to the
            // GetShaderFunction method below-- which could be helpful, but ultimately
            // redundant if that is an internal processing step.
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            // Cannot get a shader function from a context node, that needs to be
            // processed by the graph.
            // -- Though, see comment before this one, it could do more- but it'd
            // be kinda pointless. It's also pointless unless a similar strategy is
            // taken for Reference nodes- who also need a fair amount of graph
            // processing to function.
            throw new NotImplementedException();
        }
    }
}
