using System.Linq;
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
                inPort.AddTypeField().SetMetadata("_RegistryKey", type.GetRegistryKey());
                var builder = registry.GetTypeBuilder(type.GetRegistryKey());
                builder.BuildType(inPort.GetTypeField(), registry);
            }
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var port = node.GetPort(kContextEntry);
            var field = port.GetTypeField();
            var shaderType = registry.GetShaderType(field, container);

            var shaderFunctionBuilder = new ShaderFunction.Builder(container, $"refpass_{shaderType.Name}");
            shaderFunctionBuilder.AddInput(shaderType, "Input");
            shaderFunctionBuilder.AddOutput(shaderType, "Output");
            shaderFunctionBuilder.AddLine("Output = Input;");

            return shaderFunctionBuilder.Build();
        }
    }
}
