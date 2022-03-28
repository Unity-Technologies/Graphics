using System;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
   internal class ContextBuilder : INodeDefinitionBuilder
    {
        public static readonly RegistryKey kRegistryKey = new RegistryKey { Name = "_Context", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;


        static public void AddContextEntry(NodeHandler contextNode, IContextDescriptor.ContextEntry entry, Registry registry)
        {
            // TODO/Problem: Only good for GraphType
            var port = contextNode.AddPort<GraphType>(entry.fieldName, true, registry);
            GraphTypeHelpers.InitGraphType(port.GetTypeField(), entry.length, entry.precision, entry.primitive, entry.height);
            GraphTypeHelpers.SetAsMat4(port.GetTypeField(), entry.initialValue);

            port.GetTypeField().AddSubField(GraphType.kEntry, entry);

            contextNode.AddPort<GraphType>($"out_{entry.fieldName}", false, registry);
        }

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var contextKey = node.GetMetadata<RegistryKey>("_contextDescriptor");
            var context = registry.GetContextDescriptor(contextKey);
            foreach (var entry in context.GetEntries())
                AddContextEntry(node, entry, registry);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            // Cannot get a shader function from a context node, that needs to be processed by the graph.
            // -- Though, see comment before this one, it could do more- but it'd be kinda pointless.
            // It's also pointless unless a similar strategy is taken for Reference nodes- who also need a fair amount of graph processing to function.
            throw new NotImplementedException();
        }
    }
}
