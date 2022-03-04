using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphDelta;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;
using System.Collections.Generic;
using com.unity.shadergraph.defs;

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
            //public static void MathNodeDynamicResolver(
            public static void MathNodeDynamicResolver(NodeHandler node, Registry registry)
            {
                int operands = 0;
                Length resolvedLength = Length.Four;
                Height resolvedHeight = Height.One; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
                var resolvedPrimitive = Primitive.Float;
                var resolvedPrecision = Precision.Single;

                // UserData ports only exist if a user inlines a value or makes a connection.
                foreach (var port in node.GetPorts())
                {
                    if (!port.IsInput) continue;
                    operands++;
                    FieldHandler typeField = port.GetTypeField();
                    // UserData is allowed to have holes, so we should ignore what's missing.
                    FieldHandler field = typeField.GetSubField(kLength);
                    if (field != null)
                    {
                        resolvedLength = (Length)Mathf.Min((int)resolvedLength, (int)field.GetData<Length>());
                    }
                    field = typeField.GetSubField(kHeight);
                    if (field != null)
                    {
                        resolvedHeight = (Height)Mathf.Min((int)resolvedHeight, (int)field.GetData<Height>());
                    }
                    field = typeField.GetSubField(kPrecision);
                    if (field != null)
                    {
                        Precision precision = field.GetData<Precision>();
                        resolvedPrecision = (Precision)Mathf.Min((int)resolvedPrecision, (int)precision);
                    }
                    field = typeField.GetSubField(kPrimitive);
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
                        ? node.AddPort<GraphType>("Out", false, registry)
                        : node.AddPort<GraphType>($"In{i}", true, registry);

                    // Then constrain them so that type conversion in code gen can resolve the values properly.
                    port.GetTypeField().GetSubField<Length>(kLength).SetData(resolvedLength);
                    port.GetTypeField().GetSubField<Height>(kHeight).SetData(resolvedHeight);
                    port.GetTypeField().GetSubField<Primitive>(kPrimitive).SetData(resolvedPrimitive);
                    port.GetTypeField().GetSubField<Precision>(kPrecision).SetData(resolvedPrecision);
                }
            }

            internal static ShaderFunction MathNodeFunctionBuilder(
                string OpName,
                string Op,
                NodeHandler node,
                ShaderContainer container,
                Registry registry)
            {
                var outField = node.GetPort("Out").GetTypeField();
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
                NodeHelpers.MathNodeDynamicResolver(node, registry);
            }

            public ShaderFunction GetShaderFunction(
                NodeHandler node,
                ShaderContainer container,
                Registry registry)
            {
                return NodeHelpers.MathNodeFunctionBuilder("Add", "+", node, container, registry);
            }
        }

        // TODO (Brett) [BEFORE RELEASE] The doc in this class is a basic
        // description of what needs to be done to add nodes.
        // CLEAN THIS DOC
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

            /**
             * GetShaderFunction defines the output of the built
             * ShaderFoundry.ShaderFunction that results from specific
             * node data.
             */
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
    }
}
