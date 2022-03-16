using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
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
        void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry);
        ShaderFunction GetShaderFunction(INodeReader data, ShaderContainer container, Registry registry);
    }

    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(IFieldReader userData, IFieldWriter generatedData, Registry registry);
        ShaderType GetShaderType(IFieldReader data, ShaderContainer container, Registry registry);
        string GetInitializerList(IFieldReader data, Registry registry);
    }

    internal interface ICastDefinitionBuilder : IRegistryEntry
    {
        // Gross- but should suffice for now.
        (RegistryKey, RegistryKey) GetTypeConversionMapping();

        // TypeConversionMapping represents the Types we can convert, but TypeDefinitions can represent templated concepts,
        // which may mean that incompatibilities within their data could be inconvertible. Types with static fields should
        // implement an ITypeConversion with itself to ensure that static concepts can be represented.
        bool CanConvert(IFieldReader src, IFieldReader dst);

        ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderContainer container, Registry registry);
    }


    public interface IContextDescriptor : IRegistryEntry
    {
        public struct ContextEntry
        {
            public string fieldName;
            internal GraphType.Precision precision;
            internal GraphType.Primitive primitive;
            public GraphType.Length length;
            public GraphType.Height height;
            public Matrix4x4 initialValue;
            public string interpolationSemantic;
            public bool isFlat;
        }
        IReadOnlyCollection<ContextEntry> GetEntries();
    }

    internal class ReferenceNodeBuilder : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Reference", Version = 1 };
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

    internal class ContextBuilder : INodeDefinitionBuilder
    {
        public static readonly RegistryKey kRegistryKey = new RegistryKey { Name = "_Context", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            userData.GetField<RegistryKey>("_contextDescriptor", out var contextKey);
            var context = registry.GetContextDescriptor(contextKey);
            foreach (var entry in context.GetEntries())
            {
                var port = generatedData.AddPort<GraphType>(userData, entry.fieldName, true, registry);
                port.SetField(GraphType.kHeight, entry.height);
                port.SetField(GraphType.kLength, entry.length);
                port.SetField(GraphType.kPrecision, entry.precision);
                port.SetField(GraphType.kPrimitive, entry.primitive);
                port.SetField(GraphType.kEntry, entry);
                for (int i = 0; i < (int)entry.length * (int)entry.height; ++i)
                    port.SetField($"c{i}", entry.initialValue[i]);
                if (entry.interpolationSemantic == null || entry.interpolationSemantic == "")
                    port.SetField("semantic", entry.interpolationSemantic);
            }
            // We could "enfield" all of our ContextEntries into our output port, which would consequently make them accessible
            // with regards to the GetShaderFunction method below-- which could be helpful, but ultimately redundant if that is an internal processing step.
        }

        public ShaderFunction GetShaderFunction(INodeReader data, ShaderContainer container, Registry registry)
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
