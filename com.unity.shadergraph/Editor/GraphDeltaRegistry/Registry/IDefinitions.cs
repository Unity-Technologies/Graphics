using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;
using UnityEditor.ShaderGraph.Registry.Types;
using System.Linq;

namespace UnityEditor.ShaderGraph.Registry.Defs
{
    public interface IRegistryEntry
    {
        RegistryKey GetRegistryKey();

        // Some flags should be automatically populated based on the definition interface used.
        // This whole concept may prove unnecessary.
        RegistryFlags GetRegistryFlags();
    }

    internal interface INodeDefinitionBuilder : IRegistryEntry
    {
        void BuildNode(NodeHandler node, Registry registry);
        ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry);
    }

    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(FieldHandler field, Registry registry);
        ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry);
        string GetInitializerList(FieldHandler field, Registry registry);
    }

    internal interface ICastDefinitionBuilder : IRegistryEntry
    {
        // Gross- but should suffice for now.
        (RegistryKey, RegistryKey) GetTypeConversionMapping();

        // TypeConversionMapping represents the Types we can convert, but TypeDefinitions can represent templated concepts,
        // which may mean that incompatibilities within their data could be inconvertible. Types with static fields should
        // implement an ITypeConversion with itself to ensure that static concepts can be represented.
        bool CanConvert(FieldHandler src, FieldHandler dst);

        ShaderFunction GetShaderCast(FieldHandler src, FieldHandler dst, ShaderContainer container, Registry registry);
    }


    public interface IContextDescriptor : IRegistryEntry
    {
        public struct ContextEntry
        {
            public string fieldName;
            internal Types.GraphType.Precision precision;
            internal Types.GraphType.Primitive primitive;
            public Types.GraphType.Length length;
            public Types.GraphType.Height height;
            public Matrix4x4 initialValue;
        }
        IEnumerable<ContextEntry> GetEntries();
    }

    internal class ReferenceNodeBuilder : INodeDefinitionBuilder
    {
        public const string kContextEntry = "Input";
        public const string kOutput = "Output";

        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Reference", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var inPort = node.AddPort(kContextEntry, true, true);

            var connectedPort = inPort.GetConnectedPorts().FirstOrDefault();
            if (connectedPort != null)
            {
                var type = connectedPort.GetTypeField();
                node.AddPort(kOutput, false, type.GetRegistryKey(), registry);
                inPort.AddTypeField().SetMetadata("_RegistryKey", type.GetRegistryKey());
                var builder = registry.GetTypeBuilder(type.GetRegistryKey());
                builder.BuildType(inPort.GetTypeField(), registry);
            }
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var port = node.GetPort(kContextEntry);
            var field = port.GetTypeField();
            var shaderType = registry.GetShaderType(field, container);

            var shaderFunctionBuilder = new ShaderFunction.Builder(container, $"refpass_{shaderType.Name}");
            shaderFunctionBuilder.AddInput(shaderType, "Input");
            shaderFunctionBuilder.AddOutput(shaderType, "Output");
            shaderFunctionBuilder.AddLine("Output = Input;");

            return shaderFunctionBuilder.Build();
        }
    }

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










//public Block GetShaderBlock(IGraphHandler graph, INodeReader data, ShaderContainer container, Registry registry)
//{
//    var builder = new Block.Builder(data.GetRegistryKey().ToString());
//    foreach (var port in data.GetPorts())
//    {
//        if (port.IsInput() && !port.IsHorizontal())
//        {
//            // Need to be able to ask the for the previous node's built form, since that will comprise the input structure for our block.
//        }

//        if (!port.IsInput() || !port.IsHorizontal())
//            continue;

//        var varBuilder = new BlockVariable.Builder();
//        varBuilder.Type = registry.GetTypeBuilder(port.GetRegistryKey()).GetShaderType((IFieldReader)port, container, registry);
//        varBuilder.ReferenceName = port.GetName();
//        builder.AddOutput(varBuilder.Build(container));
//    }
//    var block = builder.Build(container);
//}
