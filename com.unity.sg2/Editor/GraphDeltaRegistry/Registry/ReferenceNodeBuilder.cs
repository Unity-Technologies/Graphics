using System.Linq;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    internal class ReferenceNodeBuilder : INodeDefinitionBuilder
    {
        public const string kContextEntry = "Input";
        public const string kOutput = "Output";

        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Reference", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            // Reference nodes should be initialized with a connection to this port from a context entry.
            var inPort = node.AddPort(kContextEntry, true, true);
            var outPort = node.AddPort(kOutput, false, true);

            var connectedPort = inPort.GetConnectedPorts().FirstOrDefault();
            if (connectedPort != null)
            {
                var connectedField = connectedPort.GetTypeField();
                // input port and output port's typeField data should now closely match the context entry data.
                ITypeDefinitionBuilder.CopyTypeField(connectedField, inPort.AddTypeField(), registry);
                ITypeDefinitionBuilder.CopyTypeField(connectedField, outPort.AddTypeField(), registry);
            }
            else
            {
                // This is an error state of sorts.
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
