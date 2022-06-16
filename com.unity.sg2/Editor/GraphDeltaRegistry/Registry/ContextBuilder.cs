using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Defs;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;
using static UnityEditor.ShaderGraph.GraphDelta.INodeDefinitionBuilder;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ContextBuilder : INodeDefinitionBuilder
    {
        public static readonly RegistryKey kRegistryKey = new RegistryKey {Name = "_Context", Version = 1};
        public RegistryKey GetRegistryKey() => kRegistryKey;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public static void AddContextEntry(NodeHandler contextNode, ITypeDescriptor type, string fieldName, Registry registry)
        {
            NodeBuilderUtils.ParameterDescriptorToField(
                new ParameterDescriptor(fieldName, type, GraphType.Usage.In),
                TYPE.Vec4,
                contextNode,
                registry);

            NodeBuilderUtils.ParameterDescriptorToField(
                new ParameterDescriptor($"out_{fieldName}", type, GraphType.Usage.Out),
                TYPE.Vec4,
                contextNode,
                registry);
        }

        public static void AddContextEntry(NodeHandler contextNode, ContextEntry entry, Registry registry)
        {
            // TODO/Problem: Only good for GraphType
            var inPort = contextNode.AddPort<GraphType>(entry.fieldName, true, registry);
            GraphTypeHelpers.InitGraphType(inPort.GetTypeField(), entry.length, entry.precision, entry.primitive, entry.height);
            GraphTypeHelpers.SetAsMat4(inPort.GetTypeField(), entry.initialValue);

            inPort.GetTypeField().AddSubField(GraphType.kEntry, entry);

            var outPort = contextNode.AddPort<GraphType>($"out_{entry.fieldName}", false, registry);
            ITypeDefinitionBuilder.CopyTypeField(inPort.GetTypeField(), outPort.GetTypeField(), registry);
        }

        static void SetUpReferableEntry(PortHandler port,
            PropertyBlockUsage usage = PropertyBlockUsage.Excluded,
            DataSource source = DataSource.Global,
            string displayName = null,
            string defaultValue = null)
        {
            port.AddField(kPropertyBlockUsage, usage);
            port.AddField(kDataSource, source);
            if (displayName != null)
                port.AddField(kDisplayName, displayName);
            if (defaultValue != null)
                port.AddField(kDefaultValue, defaultValue);
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
            SetUpReferableEntry(contextNode.GetPort(entry.fieldName), usage, source, displayName, defaultValue);
        }

        public static void AddReferableEntry(NodeHandler contextNode,
            ITypeDescriptor type,
            string fieldName,
            Registry registry,
            PropertyBlockUsage usage = PropertyBlockUsage.Excluded,
            DataSource source = DataSource.Global,
            string displayName = null,
            string defaultValue = null)
        {
            AddContextEntry(contextNode, type, fieldName, registry);
            SetUpReferableEntry(contextNode.GetPort(fieldName), usage, source, displayName, defaultValue);
        }

        public void BuildNode(NodeHandler node, Registry registry)
        {
            //This should do nothing, but temporarily keeping this in
            try
            {
                var contextKey = node.GetMetadata<RegistryKey>("_contextDescriptor");
                var context = registry.GetContextDescriptor(contextKey);
                foreach (var entry in context.GetEntries())
                    AddContextEntry(node, entry, registry);
            }
            catch
            {

            }
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out Dependencies deps)
        {
            // Cannot get a shader function from a context node, that needs to be processed by the graph.
            // -- Though, see comment before this one, it could do more- but it'd be kinda pointless.
            // It's also pointless unless a similar strategy is taken for Reference nodes- who also need a fair amount of graph processing to function.
            deps = new();
            return ShaderFunction.Invalid;
        }
    }
}
