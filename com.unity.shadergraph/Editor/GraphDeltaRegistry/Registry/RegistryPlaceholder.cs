using System.Linq;
using UnityEngine;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class RegistryPlaceholder
    {
        public int data;

        public RegistryPlaceholder(int d) { data = d; }
    }

    public static class NodeHelpers
    {
        // all common math operations can probably use the same resolver.
        //public static void MathNodeDynamicResolver(
        public static void MathNodeDynamicResolver(NodeHandler node, Registry registry)
        {
            int operands = 0;
            GraphType.Length resolvedLength = GraphType.Length.Four;
            GraphType.Height resolvedHeight = GraphType.Height.One; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
            var resolvedPrimitive = GraphType.Primitive.Float;
            var resolvedPrecision = GraphType.Precision.Single;

            // UserData ports only exist if a user inlines a value or makes a connection.
            foreach (var port in node.GetPorts())
            {
                if (!port.IsInput) continue;
                operands++;
                FieldHandler typeField = port.GetTypeField();
                // UserData is allowed to have holes, so we should ignore what's missing.
                FieldHandler field = typeField.GetSubField(GraphType.kLength);
                if (field != null)
                {
                    resolvedLength = (GraphType.Length)Mathf.Min((int)resolvedLength, (int)field.GetData<GraphType.Length>());
                }
                field = typeField.GetSubField(GraphType.kHeight);
                if (field != null)
                {
                    resolvedHeight = (GraphType.Height)Mathf.Min((int)resolvedHeight, (int)field.GetData<GraphType.Height>());
                }
                field = typeField.GetSubField(GraphType.kPrecision);
                if (field != null)
                {
                    GraphType.Precision precision = field.GetData<GraphType.Precision>();
                    resolvedPrecision = (GraphType.Precision)Mathf.Min((int)resolvedPrecision, (int)precision);
                }
                field = typeField.GetSubField(GraphType.kPrimitive);
                {
                    GraphType.Primitive primitive = field.GetData<GraphType.Primitive>();
                    resolvedPrimitive = (GraphType.Primitive)Mathf.Min((int)resolvedPrimitive, (int)primitive);
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
                port.GetTypeField().GetSubField<GraphType.Length>(GraphType.kLength).SetData(resolvedLength);
                port.GetTypeField().GetSubField<GraphType.Height>(GraphType.kHeight).SetData(resolvedHeight);
                port.GetTypeField().GetSubField<GraphType.Primitive>(GraphType.kPrimitive).SetData(resolvedPrimitive);
                port.GetTypeField().GetSubField<GraphType.Precision>(GraphType.kPrecision).SetData(resolvedPrecision);
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
            var typeBuilder = registry.GetTypeBuilder(GraphType.kRegistryKey);

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
}
