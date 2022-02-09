using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEngine;
using com.unity.shadergraph.defs;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Registry
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }

    namespace Types
    {
        public static class NodeHelpers
        {
            // all common math operations can probably use the same resolver.
            public static void MathNodeDynamicResolver(INodeReader userData, INodeWriter nodeWriter, Registry registry)
            {
                int operands = 0;
                int resolvedLength = 4;
                int resolvedHeight = 1; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
                var resolvedPrimitive = Primitive.Float;
                var resolvedPrecision = Precision.Single;

                // UserData ports only exist if a user inlines a value or makes a connection.
                foreach (var port in userData.GetPorts())
                {
                    if (!port.IsInput()) continue;
                    operands++;
                    // UserData is allowed to have holes, so we should ignore what's missing.
                    bool hasLength = port.GetField(kLength, out Length length);
                    bool hasHeight = port.GetField(kHeight, out Height height);
                    bool hasPrimitive = port.GetField(kPrimitive, out Primitive primitive);
                    bool hasPrecision = port.GetField(kPrecision, out Precision precision);

                    // Legacy DynamicVector's default behavior is to use the most constrained typing.
                    resolvedLength = hasLength ? Mathf.Min(resolvedLength, (int)length) : resolvedLength;
                    resolvedHeight = hasHeight ? Mathf.Min(resolvedHeight, (int)height) : resolvedHeight;
                    resolvedPrimitive = hasPrimitive ? (Primitive)Mathf.Min((int)resolvedPrimitive, (int)primitive) : resolvedPrimitive;
                    resolvedPrecision = hasPrecision ? (Precision)Mathf.Min((int)resolvedPrecision, (int)precision) : resolvedPrecision;
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
                    port.SetField(kLength, (Length)resolvedLength);
                    port.SetField(kHeight, (Height)resolvedHeight);
                    port.SetField(kPrimitive, resolvedPrimitive);
                    port.SetField(kPrecision, resolvedPrecision);
                }
            }

            internal static ShaderFoundry.ShaderFunction MathNodeFunctionBuilder(
                string OpName,
                string Op,
                INodeReader data,
                ShaderFoundry.ShaderContainer container,
                Registry registry)
            {
                data.TryGetPort("Out", out var outPort);
                var typeBuilder = registry.GetTypeBuilder(kRegistryKey);

                var shaderType = typeBuilder.GetShaderType((IFieldReader)outPort, container, registry);
                int count = data.GetPorts().Count() - 1;

                string funcName = $"{OpName}{count}_{shaderType.Name}";

                var builder = new ShaderFoundry.ShaderFunction.Builder(container, funcName);
                string body = "";
                bool firstOperand = true;
                foreach (var port in data.GetPorts())
                {
                    var name = port.GetName();
                    if (port.IsInput())
                    {
                        builder.AddInput(shaderType, name);
                        body += body == "" || firstOperand ? name : $" {Op} {name}";
                        firstOperand = false;
                    }
                    else
                    {
                        builder.AddOutput(shaderType, name);
                        body = name + " = " + body;
                    }
                }
                body += ";";

                builder.AddLine(body);
                return builder.Build();
            }
        }

        class AddNode : Defs.INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Add", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

            public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
            {
                NodeHelpers.MathNodeDynamicResolver(userData, nodeWriter, registry);
            }

            public ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
            {
                return NodeHelpers.MathNodeFunctionBuilder("Add", "+", data, container, registry);
            }
        }

        // TODO (Brett) [BEFORE RELEASE] The doc in this class is a basic
        // description of what needs to be done to add nodes.
        // CLEAN THIS DOC
        class PowNode : Defs.INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Pow", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

            /**
             * BuildNode defines the input and output ports of a node, along with
             * the port types.
             */
            public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
            {
                var portWriter = nodeWriter.AddPort<GraphType>(userData, "In", true, registry);
                portWriter.SetField<int>(kLength, 1);
                portWriter = nodeWriter.AddPort<GraphType>(userData, "Exp", true, registry);
                portWriter.SetField<int>(kLength, 1);
                portWriter = nodeWriter.AddPort<GraphType>(userData, "Out", false, registry);
                portWriter.SetField<int>(kLength, 1);
            }

            /**
             * GetShaderFunction defines the output of the built
             * ShaderFoundry.ShaderFunction that results from specific
             * node data.
             */
            public ShaderFoundry.ShaderFunction GetShaderFunction(
                INodeReader data,
                ShaderFoundry.ShaderContainer container,
                Registry registry)
            {
                // Get the HLSL type to associate with our variables in the
                // shader function we want to write.

                // In this case, all the types are the same. We only ask for the
                // type associated with the "Out" port/field.
                data.TryGetPort("Out", out var port); // TODO (Brett) This should have some error checking

                var shaderType = registry.GetShaderType((IFieldReader)port, container);

                // Get a builder from ShaderFoundry
                var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, "Pow");

                // Set up the vars in the shader function.
                // Each var in this case is the same type.
                shaderFunctionBuilder.AddInput(shaderType, "In");
                shaderFunctionBuilder.AddInput(shaderType, "Exp");
                shaderFunctionBuilder.AddOutput(shaderType, "Out");

                // Add the shader function body.
                shaderFunctionBuilder.AddLine("Out = pow(In, Exp);");

                // Return the results of ShaderFoundry's build.
                return shaderFunctionBuilder.Build();
            }
        }

        internal class GraphType : Defs.ITypeDefinitionBuilder
        {
            public static RegistryKey kRegistryKey => new RegistryKey { Name = "GraphType", Version = 1 };
            public RegistryKey GetRegistryKey() => kRegistryKey;
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

            public enum Precision { Fixed, Half, Single, Any }
            public enum Primitive { Bool, Int, Float, Any }
            public enum Length { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }
            public enum Height { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }
            public enum Usage { In, Out, Static, }

            // Values here represent a resolving priority.
            // The highest numeric value has the highest priority.
            public static readonly Dictionary<Precision, int> PrecisionToPriority = new()
            {
                { Precision.Fixed, 1 },
                { Precision.Half, 2 },
                { Precision.Single, 3 },
                { Precision.Any, -1}
            };
            public static readonly Dictionary<Primitive, int> PrimitiveToPriority = new()
            {
                { Primitive.Bool, 1 },
                { Primitive.Int, 2 },
                { Primitive.Float, 3 },
                { Primitive.Any, -1 }
            };
            public static readonly Dictionary<Length, int> LengthToPriority = new()
            {
                { Length.One, 1 },
                { Length.Two, 4 },
                { Length.Three, 3 },
                { Length.Four, 2 },
                { Length.Any, -1 }
            };
            public static readonly Dictionary<Height, int> HeightToPriority = new()
            {
                { Height.One, 1 },
                { Height.Two, 4 },
                { Height.Three, 3 },
                { Height.Four, 2 },
                { Height.Any, -1 }
            };


            public const string kPrimitive = "Primitive";
            public const string kPrecision = "Precision";
            public const string kLength = "Length";
            public const string kHeight = "Height";
            public const string kEntry = "_Entry";

            public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
            {
                // default initialize to a float4.
                typeWriter.SetField(kPrecision, Precision.Single);
                typeWriter.SetField(kPrimitive, Primitive.Float);
                typeWriter.SetField(kLength, Length.Four);
                typeWriter.SetField(kHeight, Height.One);

                // read userdata and make sure we have enough fields.
                if (!userData.GetField(kLength, out Length length))
                    length = Length.Four;
                if (!userData.GetField(kHeight, out Height height))
                    height = Height.One;

                // ensure that enough subfield values exist to represent userdata's current data.
                for (int i = 0; i < (int)length * (int)height; ++i)
                    typeWriter.SetField<float>($"c{i}", 0);
            }

            string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
            {
                data.GetField(kLength, out Length length);
                data.GetField(kHeight, out Height height);
                int l = Mathf.Clamp((int)length, 1, 4);
                int h = Mathf.Clamp((int)height, 1, 4);

                string result = $"{((Defs.ITypeDefinitionBuilder)this).GetShaderType(data, new ShaderFoundry.ShaderContainer(), registry).Name}" + "(";
                for(int i = 0; i < l * h; ++i)
                {
                    data.GetField($"c{i}", out float componentValue);
                    result += $"{componentValue}";
                    if (i != l * h - 1)
                        result += ", ";
                }
                result += ")";
                return result;
            }

            ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
            {
                data.GetField(kPrimitive, out Primitive primitive);
                data.GetField(kPrecision, out Precision precision);
                data.GetField(kLength, out Length length);
                data.GetField(kHeight, out Height height);
                int l = Mathf.Clamp((int)length, 1, 4);
                int h = Mathf.Clamp((int)height, 1, 4);

                string name = "float";

                switch(primitive)
                {
                    case Primitive.Bool: name = "bool"; break;
                    case Primitive.Int: name = "int"; break;
                    case Primitive.Float:
                        switch (precision)
                        {
                            case Precision.Fixed: name = "fixed"; break;
                            case Precision.Half: name = "half"; break;
                        }
                        break;
                }

                var shaderType = ShaderFoundry.ShaderType.Scalar(container, name);

                if (h != 1 && l != 1)
                {
                    shaderType = ShaderFoundry.ShaderType.Matrix(container, shaderType, l, h);
                }
                else
                {
                    shaderType = ShaderFoundry.ShaderType.Vector(container, shaderType, Mathf.Max(l, h));
                }
                return shaderType;
            }
        }

        internal class GraphTypeAssignment : Defs.ICastDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphTypeAssignment", Version = 1 };
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
            public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (kRegistryKey, kRegistryKey);
            public bool CanConvert(IFieldReader src, IFieldReader dst) => true;


            private static string MatrixCompNameFromIndex(int i, int d)
            {
                return $"_mm{ i / d }{ i % d }";
            }
            private static string VectorCompNameFromIndex(int i)
            {
                switch(i)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    default: return "w";
                }
            }


            ShaderFoundry.ShaderFunction Defs.ICastDefinitionBuilder.GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
            {
                // In this case, we can determine a casting operation purely from the built types. We don't actually need to analyze field data.
                // We will get precision truncation warnings though...
                var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
                var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);

                string castName = $"Cast{srcType.Name}_{dstType.Name}";
                var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
                builder.AddInput(srcType, "In");
                builder.AddOutput(dstType, "Out");

                var srcSize = srcType.IsVector ? srcType.VectorDimension : srcType.IsMatrix ? srcType.MatrixColumns * srcType.MatrixRows : 1;
                var dstSize = dstType.IsVector ? dstType.VectorDimension : dstType.IsMatrix ? dstType.MatrixColumns * dstType.MatrixRows : 1;

                string body = $"Out = {srcType.Name} {{ ";

                for (int i = 0; i < dstSize; ++i)
                {
                    if (i < srcSize)
                    {
                        if (dstType.IsMatrix) body += $"In.{MatrixCompNameFromIndex(i, dstType.MatrixColumns)}"; // are we row or column major?
                        if (dstType.IsVector) body += $"In.{VectorCompNameFromIndex(i)}";
                        if (dstType.IsScalar) body += $"In";
                    }
                    else body += "0";
                    if (i != dstSize - 1) body += ", ";
                }
                body += " };";

                builder.AddLine(body);
                return builder.Build();
            }
        }
    }
}
