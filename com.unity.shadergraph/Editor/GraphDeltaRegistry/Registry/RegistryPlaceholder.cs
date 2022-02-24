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
    }
}
