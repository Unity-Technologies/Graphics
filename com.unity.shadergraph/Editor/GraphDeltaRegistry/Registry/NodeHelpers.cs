using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public static class NodeHelpers
    {
        // all common math operations can probably use the same resolver.
        public static void MathNodeDynamicResolver(INodeReader userData, INodeWriter nodeWriter, Registry registry)
        {
            int operands = 0;
            int resolvedLength = 4;
            int resolvedHeight = 1; // bump this to 4 to support matrices, but inlining a matrix on a port value is weird.
            var resolvedPrimitive = GraphType.Primitive.Float;
            var resolvedPrecision = GraphType.Precision.Single;

            // UserData ports only exist if a user inlines a value or makes a connection.
            foreach (var port in userData.GetPorts())
            {
                if (!port.IsInput()) continue;
                operands++;
                // UserData is allowed to have holes, so we should ignore what's missing.
                bool hasLength = port.GetField(GraphType.kLength, out GraphType.Length length);
                bool hasHeight = port.GetField(GraphType.kHeight, out GraphType.Height height);
                bool hasPrimitive = port.GetField(GraphType.kPrimitive, out GraphType.Primitive primitive);
                bool hasPrecision = port.GetField(GraphType.kPrecision, out GraphType.Precision precision);

                // Legacy DynamicVector's default behavior is to use the most constrained typing.
                resolvedLength = hasLength ? Mathf.Min(resolvedLength, (int)length) : resolvedLength;
                resolvedHeight = hasHeight ? Mathf.Min(resolvedHeight, (int)height) : resolvedHeight;
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
                port.SetField(GraphType.kLength, (GraphType.Length)resolvedLength);
                port.SetField(GraphType.kHeight, (GraphType.Height)resolvedHeight);
                port.SetField(GraphType.kPrimitive, resolvedPrimitive);
                port.SetField(GraphType.kPrecision, resolvedPrecision);
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
            var typeBuilder = registry.GetTypeBuilder(GraphType.kRegistryKey);

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
}
