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

        void INodeDefinitionBuilder.BuildNode(NodeHandler node, Registry registry)
        {
            node.AddPort<GraphType>(kInputA, true, registry);
            node.AddPort<GraphType>(kInputB, true, registry);
            node.AddPort<GraphType>(kOutput, false, registry);

            // initialize all of our ports to be fully dynamic, so they can get resolved by their input connections.
            foreach(var port in node.GetPorts())
            {
                GraphTypeHelpers.InitGraphType(
                    port.GetTypeField(),
                    lengthDynamic: true,
                    heightDynamic: true,
                    primitiveDynamic: true,
                    precisionDynamic: true);
            }

            GraphTypeHelpers.ResolveDynamicPorts(node);
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

