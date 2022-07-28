using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal class MultiplyNode : INodeDefinitionBuilder
    {
        RegistryFlags IRegistryEntry.GetRegistryFlags() => RegistryFlags.Func;
        RegistryKey IRegistryEntry.GetRegistryKey() => new RegistryKey { Name = "Multiply", Version = 1 };

        #region Names
        public readonly static string kInputA = "A";
        public readonly static string kInputB = "B";
        public readonly static string kOutput = "Output";
        #endregion

        #region Calc
        private static int truncate(int a, int b) // ignore scalars for truncation.
            => a == 1 ? b : b == 1 ? a : Mathf.Min(a, b);
        private static int resolve(int dim, int resolved) // a scalar dimension ignores the resolved type.
            => dim == 1 ? 1 : Mathf.Min(dim, resolved);

        private static void GetDim(FieldHandler field, out int length, out int height)
        {
            height = (int)GraphTypeHelpers.GetHeight(field);
            length = (int)GraphTypeHelpers.GetLength(field);
        }

        private static bool calcResolve(NodeHandler node, out int length, out int height)
        {
            length = 1;
            height = 1;

            var inputPorts = node.GetPorts().Where(e => e.IsInput);
            var connectedFields = inputPorts.Select(e => e.GetConnectedPorts().FirstOrDefault()?.GetTypeField()).Where(e => e != null);
            bool hasVector = inputPorts.Count() != connectedFields.Count();

            foreach (var field in connectedFields)
            {
                // TODO: resolve primitive/precision.
                GetDim(field, out var fieldLength, out var fieldHeight);
                length = truncate(length, fieldLength);
                height = truncate(truncate(length, height), fieldHeight);
                hasVector |= fieldLength > 1 && fieldHeight == 1;
            }
            return hasVector;
        }

        #endregion

        void INodeDefinitionBuilder.BuildNode(NodeHandler node, Registry registry)
        {
            node.AddPort<GraphType>(kInputA, true, registry);
            node.AddPort<GraphType>(kInputB, true, registry);
            node.AddPort<GraphType>(kOutput, false, registry);

            // if there is an input vector, as a result of a disconnected port or a connection,
            // the output port cannot be a matrix.
            bool hasVector = calcResolve(node, out var resolvedLength, out var resolvedHeight);

            foreach(var port in node.GetPorts())
            {
                int length = resolvedLength;
                int height = hasVector ? 1 : resolvedHeight;
                var connectedField = port.IsInput ? port.GetConnectedPorts().FirstOrDefault()?.GetTypeField() : null;

                // If we're an input port, our type changes based on truncation rules.
                if (port.IsInput && connectedField != null)
                {
                    GetDim(connectedField, out length, out height);
                    length = resolve(length, resolvedLength);
                    height = resolve(height, resolvedHeight);
                }

                // TODO: primitive & precision
                GraphTypeHelpers.InitGraphType(port.GetTypeField(), length:(GraphType.Length)length, height:(GraphType.Height)height);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies deps)
        {
            deps = new();
            var builder = new ShaderFunction.Builder(container, "Multiply");

            bool hasMatrix = false;
            System.Text.StringBuilder result = new($"{kOutput} = ");

            foreach(var port in node.GetPorts())
            {
                var field = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(field.GetRegistryKey()).GetShaderType(field, container, registry);
                hasMatrix |= shaderType.IsMatrix;
                var param = new FunctionParameter.Builder(container, port.LocalID, shaderType, input: port.IsInput, output: !port.IsInput).Build();
                builder.AddParameter(param);
            }

            if (hasMatrix)
            {
                result.Append($"mul({kInputA}, {kInputB});");
            }
            else // component-wise
            {
                result.Append($"{kInputA} * {kInputB};");
            }

            builder.AddLine(result.ToString());
            return builder.Build();
        }
    }
}

