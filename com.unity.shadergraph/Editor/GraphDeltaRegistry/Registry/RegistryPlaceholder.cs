using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphDelta;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace UnityEditor.ShaderGraph.Registry
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }

    namespace Types
    {
        ////public class MakeNode : Defs.INodeDefinitionBuilder
        ////{
        ////    public RegistryKey GetRegistryKey() => new RegistryKey { Name = "MakeType", Version = 1 };
        ////    public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        ////    public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
        ////    {
        ////        // We just have a field for now that indicates what our type is.
        ////        userData.GetField("Type", out RegistryKey key);

        ////        // Erroneous if Key is default or not a Type, but we have no error msging yet.

        ////        var inport = nodeWriter.AddPort(userData, "In", true, key, registry);
        ////        var outport = nodeWriter.AddPort(userData, "Out", false, key, registry);
        ////        inport.TryAddConnection(outport);

        ////        // To be able to support nested types (eg. TypeDefs of TypeDefs),
        ////        // we'll need to be able to concretize and iterate over fields to promote them to ports properly,
        ////        // iterating over fields would mean reading from the concretized layer-- don't currently have a way to get a reader from that in the builder.
        ////    }
        ////}
        ///

        public static class NodeHelpers
        {
            // all common math operations can probably use the same resolver.
            //public static void MathNodeDynamicResolver(
            public static void MathNodeDynamicResolver(NodeHandler node)
            {
                int operands = 0;
                int resolvedLength = 4;
                int resolvedHeight = 1; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
                var resolvedPrimitive = Primitive.Float;
                var resolvedPrecision = Precision.Full;

                // UserData ports only exist if a user inlines a value or makes a connection.
                foreach (var port in node.GetPorts())
                {
                    if (!port.IsInput) continue;
                    operands++;
                    // UserData is allowed to have holes, so we should ignore what's missing.
                    FieldHandler field = port.GetField(kLength);
                    if (field != null)
                    {
                        resolvedLength = Mathf.Min(resolvedLength, field.GetData<int>());
                    }
                    field = port.GetField(kHeight);
                    if (field != null)
                    {
                        resolvedHeight = Mathf.Min(resolvedHeight, field.GetData<int>());
                    }
                    field = port.GetField(kPrecision);
                    if (field != null)
                    {
                        Precision precision = field.GetData<Precision>();
                        resolvedPrecision = (Precision)Mathf.Min((int)resolvedPrecision, (int)precision);
                    }
                    field = port.GetField(kPrimitive);
                    {
                        Primitive primitive = field.GetData<Primitive>();
                        resolvedPrimitive = (Primitive)Mathf.Min((int)resolvedPrimitive, (int)primitive);
                    }
                }

                // We need at least 2 input ports or 1 more than the existing number of connections.
                operands = Mathf.Max(1, operands) + 1;

                // Need to concretize each port so that they exist.
                for (int i = 0; i < operands + 1; ++i)
                {
                    // Output port gets constrained the same way.
                    var port = i == 0
                        ? node.AddPort("Out", false, true)
                        : node.AddPort($"In{i}", true, true);

                    // Then constrain them so that type conversion in code gen can resolve the values properly.
                    port.AddField(kLength, resolvedLength);
                    port.AddField(kHeight, resolvedHeight);
                    port.AddField(kPrimitive, resolvedPrimitive);
                    port.AddField(kPrecision, resolvedPrecision);
                }
            }

            internal static ShaderFunction MathNodeFunctionBuilder(
                string OpName,
                string Op,
                NodeHandler node,
                ShaderContainer container,
                Registry registry)
            {
                var outField = node.GetField("Out");
                var typeBuilder = registry.GetTypeBuilder(kRegistryKey);

                var shaderType = typeBuilder.GetShaderType(outField, container, registry);
                int count = node.GetPorts().Count() - 1;

                string funcName = $"{OpName}{count}_{shaderType.Name}";

                var builder = new ShaderFunction.Builder(container, funcName);
                string body = "";
                bool firstOperand = true;
                foreach (var port in node.GetPorts())
                {
                    var name = port.LocalID;
                    if (port.IsInput)
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

            public void BuildNode(NodeHandler node, Registry registry)
            {
                NodeHelpers.MathNodeDynamicResolver(node);
            }

            public ShaderFunction GetShaderFunction(
                NodeHandler node,
                ShaderContainer container,
                Registry registry)
            {
                return NodeHelpers.MathNodeFunctionBuilder("Add", "+", node, container, registry);
            }
        }

        class PowNode : Defs.INodeDefinitionBuilder
        {
            public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Pow", Version = 1 };

            public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

            public void BuildNode(NodeHandler node, Registry registry)
            {
                var port = node.AddPort("In", true, true);
                port.AddField(kLength, 1);
                port = node.AddPort("Exp", true, true);
                port.AddField(kLength, 1);
                port = node.AddPort("Out", false, true);
                port.AddField(kLength, 1);
            }

            public ShaderFunction GetShaderFunction(
                NodeHandler node,
                ShaderContainer container,
                Registry registry)
            {
                FieldHandler field = node.GetField("Out");

                var shaderType = registry.GetShaderType(field, container);

                // Get a builder from ShaderFoundry
                var shaderFunctionBuilder = new ShaderFunction.Builder(container, "Pow");

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
            public static RegistryKey kRegistryKey => new() { Name = "GraphType", Version = 1 };
            public RegistryKey GetRegistryKey() => kRegistryKey;
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

            public enum Precision { Fixed, Half, Full };
            public enum Primitive { Bool, Int, Float };

            public const string kPrimitive = "Primitive";
            public const string kPrecision = "Precision";
            public const string kLength = "Length";
            public const string kHeight = "Height";
            public const string kEntry = "_Entry";

            public void BuildType(FieldHandler field, Registry registry)
            {
                // default initialize to a float4
                field.AddSubField(k_concrete, kPrecision, Precision.Full);
                field.AddSubField(k_concrete, kPrimitive, Primitive.Float);
                field.AddSubField(k_concrete, kLength, 4);
                field.AddSubField(k_concrete, kHeight, 1);

                // ensure that enough subfield values exist to represent userdata's current data
                for (int i = 0; i < 16; ++i)
                    field.AddSubField<float>(k_concrete, $"c{i}", 0);
            }

            public string GetInitializerList(FieldHandler field, Registry registry)
            {
                var subField = field.GetSubField<int>(kLength);
                int length = subField == null ? 0 : subField.GetData();
                subField = field.GetSubField<int>(kHeight);
                int height = subField == null ? 0 : subField.GetData();
                length = Mathf.Clamp(length, 1, 4);
                height = Mathf.Clamp(height, 1, 4);

                ShaderType shaderType = GetShaderType(field, new ShaderContainer(), registry);
                string result = $"{shaderType.Name}" + "(";
                for (int i = 0; i < length * height; ++i)
                {
                    float componentValue = field.GetSubField<float>($"c{i}").GetData();
                    result += $"{componentValue}";
                    if (i != length * height - 1)
                        result += ", ";
                }
                result += ")";
                return result;
            }

            public ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry)
            {
                var primitiveSubField = field.GetSubField<Primitive>(kPrimitive);
                Primitive primitive = Primitive.Float;
                if (primitiveSubField != null)
                {
                    primitive = primitiveSubField.GetData();
                }
                var precisionSubField = field.GetSubField<Precision>(kPrecision);
                Precision precision = Precision.Full;
                if (precisionSubField != null)
                {
                    precision = precisionSubField.GetData();
                }

                var lengthSubField = field.GetSubField<int>(kLength);
                int length = lengthSubField == null ? 0 : lengthSubField.GetData();
                var heightSubField = field.GetSubField<int>(kHeight);
                int height = heightSubField == null ? 0 : heightSubField.GetData();

                length = Mathf.Clamp(length, 1, 4);
                height = Mathf.Clamp(height, 1, 4);

                string name = "float";

                switch (primitive)
                {
                    case Primitive.Bool:
                        name = "bool";
                        break;
                    case Primitive.Int:
                        name = "int";
                        break;
                    case Primitive.Float:
                        switch (precision)
                        {
                            case Precision.Fixed:
                                name = "fixed";
                                break;
                            case Precision.Half:
                                name = "half";
                                break;
                        }
                        break;
                }

                var shaderType = ShaderType.Scalar(container, name);

                if (height != 1 && length != 1)
                {
                    shaderType = ShaderType.Matrix(container, shaderType, length, height);
                }
                else
                {
                    length = Mathf.Max(length, height);
                    shaderType = ShaderType.Vector(container, shaderType, length);
                }
                return shaderType;
            }
        }

        //internal class GraphTypeAssignment : Defs.ICastDefinitionBuilder
        //{
        //    public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphTypeAssignment", Version = 1 };
        //    public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        //    public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (kRegistryKey, kRegistryKey);
        //    public bool CanConvert(IFieldReader src, IFieldReader dst) => true;


        //    private static string MatrixCompNameFromIndex(int i, int d)
        //    {
        //        return $"_mm{ i / d }{ i % d }";
        //    }
        //    private static string VectorCompNameFromIndex(int i)
        //    {
        //        switch(i)
        //        {
        //            case 0: return "x";
        //            case 1: return "y";
        //            case 2: return "z";
        //            default: return "w";
        //        }
        //    }


        //    ShaderFunction Defs.ICastDefinitionBuilder.GetShaderCast(IFieldReader src, IFieldReader dst, ShaderContainer container, Registry registry)
        //    {
        //        // In this case, we can determine a casting operation purely from the built types. We don't actually need to analyze field data.
        //        // We will get precision truncation warnings though...
        //        var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
        //        var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);

        //        string castName = $"Cast{srcType.Name}_{dstType.Name}";
        //        var builder = new ShaderFunction.Builder(container, castName);
        //        builder.AddInput(srcType, "In");
        //        builder.AddOutput(dstType, "Out");

        //        var srcSize = srcType.IsVector ? srcType.VectorDimension : srcType.IsMatrix ? srcType.MatrixColumns * srcType.MatrixRows : 1;
        //        var dstSize = dstType.IsVector ? dstType.VectorDimension : dstType.IsMatrix ? dstType.MatrixColumns * dstType.MatrixRows : 1;

        //        string body = $"Out = {srcType.Name} {{ ";

        //        for (int i = 0; i < dstSize; ++i)
        //        {
        //            if (i < srcSize)
        //            {
        //                if (dstType.IsMatrix) body += $"In.{MatrixCompNameFromIndex(i, dstType.MatrixColumns)}"; // are we row or column major?
        //                if (dstType.IsVector) body += $"In.{VectorCompNameFromIndex(i)}";
        //                if (dstType.IsScalar) body += $"In";
        //            }
        //            else body += "0";
        //            if (i != dstSize - 1) body += ", ";
        //        }
        //        body += " };";

        //        builder.AddLine(body);
        //        return builder.Build();
        //    }
        //}
    }
}
