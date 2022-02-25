using UnityEngine;
using System;
using System.Collections.Generic;

using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderFoundry;

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
        void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry);
        ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry);
    }

    internal interface ITypeDefinitionBuilder : IRegistryEntry
    {
        void BuildType(IFieldReader userData, IFieldWriter generatedData, Registry registry);
        ShaderFoundry.ShaderType GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry);
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

        ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry);
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
                var port = generatedData.AddPort<Types.GraphType>(userData, entry.fieldName, true, registry);
                port.SetField(Types.GraphType.kHeight, entry.height);
                port.SetField(Types.GraphType.kLength, entry.length);
                port.SetField(Types.GraphType.kPrecision, entry.precision);
                port.SetField(Types.GraphType.kPrimitive, entry.primitive);
                port.SetField(Types.GraphType.kEntry, entry);
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










//public ShaderFoundry.Block GetShaderBlock(IGraphHandler graph, INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
//{
//    var builder = new ShaderFoundry.Block.Builder(data.GetRegistryKey().ToString());
//    foreach (var port in data.GetPorts())
//    {
//        if (port.IsInput() && !port.IsHorizontal())
//        {
//            // Need to be able to ask the for the previous node's built form, since that will comprise the input structure for our block.
//        }

//        if (!port.IsInput() || !port.IsHorizontal())
//            continue;

//        var varBuilder = new ShaderFoundry.BlockVariable.Builder();
//        varBuilder.Type = registry.GetTypeBuilder(port.GetRegistryKey()).GetShaderType((IFieldReader)port, container, registry);
//        varBuilder.ReferenceName = port.GetName();
//        builder.AddOutput(varBuilder.Build(container));
//    }
//    var block = builder.Build(container);
//}
