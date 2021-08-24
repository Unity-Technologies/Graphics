using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }

    namespace Types
    {
        public class MakeNode : Defs.INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "MakeType", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

            public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
            {
                // We just have a field for now that indicates what our type is.
                userData.GetField("Type", out RegistryKey key);

                // Erroneous if Key is default or not a Type, but we have no error msging yet.

                var inport = nodeWriter.AddPort(userData, "In", true, key, registry);
                var outport = nodeWriter.AddPort(userData, "Out", false, key, registry);
                inport.TryAddConnection(outport);

                // To be able to support nested types (eg. TypeDefs of TypeDefs),
                // we'll need to be able to concretize and iterate over fields to promote them to ports properly,
                // iterating over fields would mean reading from the concretized layer-- don't currently have a way to get a reader from that in the builder.
            }
        }

        public class AddNode : Defs.INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Add", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

            public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
            {
                int operands = 0;
                int resolvedLength = 4;
                int resolvedHeight = 1; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
                var resolvedPrimitive = GraphType.Primitive.Float;
                var resolvedPrecision = GraphType.Precision.Full;

                // UserData ports only exist if a user inlines a value or makes a connection.
                foreach (var port in userData.GetPorts())
                {
                    if (!port.IsInput()) continue;
                    operands++;
                    // UserData is allowed to have holes, so we should ignore what's missing.
                    bool hasLength = port.GetField(GraphType.kLength, out int length);
                    bool hasHeight = port.GetField(GraphType.kHeight, out int height);
                    bool hasPrimitive = port.GetField(GraphType.kPrimitive, out GraphType.Primitive primitive);
                    bool hasPrecision = port.GetField(GraphType.kPrecision, out GraphType.Precision precision);

                    // Legacy DynamicVector's default behavior is to use the most constrained typing.
                    resolvedLength = hasLength ? Mathf.Min(resolvedLength, length) : resolvedLength;
                    resolvedHeight = hasHeight ? Mathf.Min(resolvedHeight, height) : resolvedHeight;
                    resolvedPrimitive = hasPrimitive ? (GraphType.Primitive)Mathf.Min((int)resolvedPrimitive, (int)primitive) : resolvedPrimitive;
                    resolvedPrecision = hasPrecision ? (GraphType.Precision)Mathf.Min((int)resolvedPrecision, (int)precision) : resolvedPrecision;
                }
                // We need at least 2 input ports or 1 more than the existing number of connections.
                operands = Mathf.Max(1, operands) + 1;

                // Need to concretize each port so that they exist.
                for (int i = 0; i < operands + 1; ++i)
                {
                    // Output port gets constrained the same way.
                    var port = i == 0
                            ? nodeWriter.AddPort<GraphType>(userData, "Out", false, registry)
                            : nodeWriter.AddPort<GraphType>(userData, $"In{i}", true, registry);

                    // Then constrain them so that type conversion in code gen can resolve the values properly.
                    port.SetField(GraphType.kLength, resolvedLength);
                    port.SetField(GraphType.kHeight, resolvedHeight);
                    port.SetField(GraphType.kPrimitive, resolvedPrimitive);
                    port.SetField(GraphType.kPrecision, resolvedPrecision);
                }
            }
        }

        public class GraphType : Defs.ITypeDefinitionBuilder
        {
            public static RegistryKey kRegistryKey => new RegistryKey { Name = "GraphType", Version = 1 };
            public RegistryKey GetRegistryKey() => kRegistryKey;
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

            public enum Precision {Fixed, Half, Full };
            public enum Primitive { Bool, Int, Float };

            public const string kPrimitive = "Primitive";
            public const string kPrecision = "Precision";
            public const string kLength = "Length";
            public const string kHeight = "Height";

            public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
            {
                // default initialize to a float4.
                typeWriter.SetField(kPrecision, Precision.Full);
                typeWriter.SetField(kPrimitive, Primitive.Float);
                typeWriter.SetField(kLength, 4);
                typeWriter.SetField(kHeight, 1);

                // read userdata and make sure we have enough fields.
                if (!userData.GetField(kLength, out int length))
                    length = 4;
                if (!userData.GetField(kHeight, out int height))
                    height = 1;

                // ensure that enough subfield values exist to represent userdata's current data.
                for (int i = 0; i < length * height; ++i)
                    typeWriter.SetField<float>($"c{i}", 0);
            }
        }

        public class GraphTypeAssignment : Defs.ICastDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphTypeAssignment", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
            public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GraphType.kRegistryKey, GraphType.kRegistryKey);
            public bool CanConvert(IFieldReader src, IFieldReader dst) => true;
        }
    }
}
