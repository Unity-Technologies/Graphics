using System;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal class ReferenceNodeBuilder : INodeDefinitionBuilder
    {
        public const string kContextEntry = "Input";
        public const string kOutput = "Output";

        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Reference", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var inPort = node.AddPort(kContextEntry, true, true);

            var connectedPort = inPort.GetConnectedPorts().FirstOrDefault();
            if (connectedPort != null)
            {
                var type = connectedPort.GetTypeField();
                node.AddPort(kOutput, false, type.GetRegistryKey(), registry);
            }
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var port = node.GetPort(kContextEntry);
            var field = port.GetTypeField();
            var shaderType = registry.GetShaderType(field, container);

            var shaderFunctionBuilder = new ShaderFunction.Builder(container, $"refpass_{shaderType.Name}");
            shaderFunctionBuilder.AddInput(shaderType, "In");
            shaderFunctionBuilder.AddOutput(shaderType, "Out");
            shaderFunctionBuilder.AddLine("Out = In;");

            return shaderFunctionBuilder.Build();
        }
    }
}
