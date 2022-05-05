using System;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// FunctionDescriptorNodeBuilder is a way to to make INodeDefinitionBuilder
    /// instances from FunctionDescriptors.
    ///
    /// This is used to load the standard node defintions into the Registry.
    /// (See: StandardNodeDefinitions)
    /// </summary>
    internal class FunctionDescriptorNodeBuilder : INodeDefinitionBuilder
    {
        private readonly FunctionDescriptor m_functionDescriptor;

        internal FunctionDescriptorNodeBuilder(FunctionDescriptor fd)
        {
            m_functionDescriptor = fd; // copy
        }

        public void BuildNode(NodeHandler node, Registry registry)
        {
            ParametricTypeDescriptor fallbackType = NodeBuilderUtils.FallbackTypeResolver(node);
            foreach (var param in m_functionDescriptor.Parameters)
            {
                NodeBuilderUtils.ParameterDescriptorToField(
                    param,
                    fallbackType,
                    node,
                    registry);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            NodeHandler data,
            ShaderContainer container,
            Registry registry)
        {
            // Get a builder from ShaderFoundry
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, m_functionDescriptor.Name);

            // Find the name of a texture port.
            // This texture variable name is used for every unassigned sampler state
            // in the shader function.
            string texturePortName = null;

            // Set up the vars in the shader function.
            foreach (var param in m_functionDescriptor.Parameters)
            {
                // overwrite the stored texture port name each time a texture type is found.
                if (param.TypeDescriptor is TextureTypeDescriptor)
                {
                    texturePortName = param.Name;
                }
                var port = data.GetPort(param.Name);
                var field = port.GetTypeField();
                var shaderType = registry.GetShaderType(field, container);
                if (param.Usage == GraphType.Usage.In || param.Usage == GraphType.Usage.Static || param.Usage == GraphType.Usage.Local)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == GraphType.Usage.Out)
                {
                    shaderFunctionBuilder.AddOutput(shaderType, param.Name);
                }
                else
                {
                    throw new Exception($"No ShaderFunction parameter type for {param.Usage}");
                }
            }

            // output a texture assignment for each sampler state that is unassigned
            foreach (var param in m_functionDescriptor.Parameters)
            {
                if (param.TypeDescriptor is not SamplerStateTypeDescriptor) continue;
                var samplerPort = data.GetPort(param.Name);
                bool isConnected = samplerPort.GetConnectedPorts().Count() != 0;
                bool isInitialized = SamplerStateType.IsInitialized(samplerPort.GetTypeField());
                if (!isConnected && !isInitialized && !string.IsNullOrEmpty(texturePortName))
                {
                    string initSampler = $"{param.Name}.samplerstate = {texturePortName}.samplerstate;";
                    shaderFunctionBuilder.AddLine(initSampler);
                }
            }

            // Add the shader function body.
            shaderFunctionBuilder.AddLine(m_functionDescriptor.Body);

            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        public RegistryKey GetRegistryKey()
        {
            return new RegistryKey
            {
                Name = m_functionDescriptor.Name,
                Version = m_functionDescriptor.Version
            };
        }

        public RegistryFlags GetRegistryFlags()
        {
            return RegistryFlags.Func;
        }
    }
}
