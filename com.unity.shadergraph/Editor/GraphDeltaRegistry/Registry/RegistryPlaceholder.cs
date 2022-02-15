using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

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
            public static void MathNodeDynamicResolver(INodeReader userData, INodeWriter nodeWriter, Registry registry)
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

            internal static ShaderFunction MathNodeFunctionBuilder(
                string OpName,
                string Op,
                INodeReader data,
                ShaderContainer container,
                Registry registry)
            {
                data.TryGetPort("Out", out var outPort);
                var typeBuilder = registry.GetTypeBuilder(GraphType.kRegistryKey);

                var shaderType = typeBuilder.GetShaderType((IFieldReader)outPort, container, registry);
                int count = data.GetPorts().Count() - 1;

                string funcName = $"{OpName}{count}_{shaderType.Name}";

                var builder = new ShaderFunction.Builder(container, funcName);
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

            public ShaderFunction GetShaderFunction(INodeReader data, ShaderContainer container, Registry registry)
            {
                return NodeHelpers.MathNodeFunctionBuilder("Add", "+", data, container, registry);
            }
        }

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
                portWriter.SetField<int>(GraphType.kLength, 1);
                portWriter = nodeWriter.AddPort<GraphType>(userData, "Exp", true, registry);
                portWriter.SetField<int>(GraphType.kLength, 1);
                portWriter = nodeWriter.AddPort<GraphType>(userData, "Out", false, registry);
                portWriter.SetField<int>(GraphType.kLength, 1);
            }

            /**
             * GetShaderFunction defines the output of the built 
             * ShaderFunction that results from specific
             * node data.
             */
            public ShaderFunction GetShaderFunction(
                INodeReader data,
                ShaderContainer container,
                Registry registry)
            {
                // Get the HLSL type to associate with our variables in the
                // shader function we want to write.

                // In this case, all the types are the same. We only ask for the
                // type associated with the "Out" port/field.
                data.TryGetPort("Out", out var port); // TODO (Brett) This should have some error checking

                var shaderType = registry.GetShaderType((IFieldReader)port, container);

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
            public static RegistryKey kRegistryKey => new RegistryKey { Name = "GraphType", Version = 1 };
            public RegistryKey GetRegistryKey() => kRegistryKey;
            public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

            public enum Precision {Fixed, Half, Full };
            public enum Primitive { Bool, Int, Float };

            public const string kPrimitive = "Primitive";
            public const string kPrecision = "Precision";
            public const string kLength = "Length";
            public const string kHeight = "Height";
            public const string kEntry = "_Entry";

            public void BuildType(IFieldHandler field, Registry registry)
            //public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
            {
                // TODO (Brett) This is going to change to accepting a layer

                // default initialize to a float4.
                field.AddSubField(kPrecision, Precision.Full);
                field.AddSubField(kPrimitive, Primitive.Float);
                field.AddSubField(kLength, 4);
                field.AddSubField(kHeight, 1);

                //field.GetSubField<Precision>(kPrecision).GetData()

                // read userdata and make sure we have enough fields.
                int length = field.GetSubField(kLength);
                if (!userData.GetField(kLength, out int length))
                    length = 4;
                if (!userData.GetField(kHeight, out int height))
                    height = 1;

                // ensure that enough subfield values exist to represent userdata's current data.
                for (int i = 0; i < length * height; ++i)
                    typeWriter.SetField<float>($"c{i}", 0);
            }

            string GetInitializerList(GraphDataHandler field, Registry registry)
            //string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
            {

                data.GetField(kLength, out int length);
                data.GetField(kHeight, out int height);
                length = Mathf.Clamp(length, 1, 4);
                height = Mathf.Clamp(height, 1, 4);

                string result = $"{((Defs.ITypeDefinitionBuilder)this).GetShaderType(data, new ShaderContainer(), registry).Name}" + "(";
                for(int i = 0; i < length*height; ++i)
                {
                    data.GetField($"c{i}", out float componentValue);
                    result += $"{componentValue}";
                    if (i != length * height - 1)
                        result += ", ";
                }
                result += ")";
                return result;
            }

            ShaderType GetShaderType(GraphDataHandler field, ShaderContainer container, Registry registry)
            //ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderContainer container, Registry registry)
            {
                Primitive primitive = field.GetSubField(kPrimitive);
                Precision precision = data.GetField(kPrecision, out Precision precision);
                data.GetField(kLength, out int length);
                data.GetField(kHeight, out int height);
                length = Mathf.Clamp(length, 1, 4);
                height = Mathf.Clamp(height, 1, 4);

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
        //    public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GraphType.kRegistryKey, GraphType.kRegistryKey);
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
