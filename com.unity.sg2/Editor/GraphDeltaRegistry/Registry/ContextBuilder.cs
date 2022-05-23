using System;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ContextBuilder : INodeDefinitionBuilder
    {
        public static readonly RegistryKey kRegistryKey = new RegistryKey { Name = "_Context", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;


        static public void AddContextEntry(NodeHandler contextNode, ContextEntry entry, Registry registry)
        {
            // TODO/Problem: Only good for GraphType
            var inPort = contextNode.AddPort<GraphType>(entry.fieldName, true, registry);
            GraphTypeHelpers.InitGraphType(inPort.GetTypeField(), entry.length, entry.precision, entry.primitive, entry.height);
            GraphTypeHelpers.SetAsMat4(inPort.GetTypeField(), entry.initialValue);

            inPort.GetTypeField().AddSubField(GraphType.kEntry, entry);

            var outPort = contextNode.AddPort<GraphType>($"out_{entry.fieldName}", false, registry);
            ITypeDefinitionBuilder.CopyTypeField(inPort.GetTypeField(), outPort.GetTypeField(), registry);
        }

        public static void AddReferableEntry(NodeHandler contextNode,
                                             ContextEntry entry,
                                             Registry registry,
                                             PropertyBlockUsage usage = PropertyBlockUsage.Excluded,
                                             DataSource source = DataSource.Global,
                                             string displayName = null,
                                             string defaultValue = null)
        {
            AddContextEntry(contextNode, entry, registry);
            var port = contextNode.GetPort(entry.fieldName);
            port.AddField(kPropertyBlockUsage, usage);
            port.AddField(kDataSource, source);
            if(displayName != null)
                port.AddField(kDisplayName, displayName);
            if(defaultValue != null)
                port.AddField(kDefaultValue, defaultValue);

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
            return ShaderFunction.Invalid;
        }
    }
}
