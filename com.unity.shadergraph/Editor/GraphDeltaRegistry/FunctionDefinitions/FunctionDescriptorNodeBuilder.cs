using System;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEditor.ShaderGraph.Registry.Types;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// </summary>
    internal class FunctionDescriptorNodeBuilder : INodeDefinitionBuilder
    {
        private readonly FunctionDescriptor m_functionDescriptor;

        /// <summary>
        /// </summary>
        public static IPortWriter ParameterDescriptorToField(
            ParameterDescriptor param,
            INodeReader nodeReader,
            INodeWriter nodeWriter,
            Registry registry)
        {
            IPortWriter port = nodeWriter.AddPort<GraphType>(
                nodeReader,
                param.Name,
                param.Usage == Usage.In,
                registry
            );

            port.SetField(GraphType.kLength, param.TypeDescriptor.Length);
            port.SetField(GraphType.kHeight, param.TypeDescriptor.Height);
            port.SetField(GraphType.kPrecision, param.TypeDescriptor.Precision);
            port.SetField(GraphType.kPrimitive, param.TypeDescriptor.Primitive);

            return port;
        }

        internal FunctionDescriptorNodeBuilder(FunctionDescriptor fd)
        {
            m_functionDescriptor = fd; // copy
        }

        public void BuildNode(
            INodeReader userData,
            INodeWriter generatedData,
            Registry registry)
        {
            foreach (var param in m_functionDescriptor.Parameters)
            {
                ParameterDescriptorToField(param, userData, generatedData, registry);
            }
        }

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            INodeReader data,
            ShaderContainer container,
            Registry registry)
        {
            // Get the ShaderType for the first output port we find.
            string outPortName = null;
            foreach (var param in m_functionDescriptor.Parameters)
            {
                if (param.Usage == Usage.Out)
                {
                    outPortName = param.Name;
                    break;
                }
            }
            if (outPortName == null)
            {
                // No out port was found.
                throw new Exception("No output port found for ");
            }

            data.TryGetPort(outPortName, out var port);
            var shaderType = registry.GetShaderType((IFieldReader)port, container);

            // Get a builder from ShaderFoundry
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, m_functionDescriptor.Name);

            // Set up the vars in the shader function.
            foreach (var param in m_functionDescriptor.Parameters)
            {
                if (param.Usage == Usage.In || param.Usage == Usage.Static)
                {
                    shaderFunctionBuilder.AddInput(shaderType, param.Name);
                }
                else if (param.Usage == Usage.Out)
                {
                    shaderFunctionBuilder.AddOutput(shaderType, param.Name);
                }
                else
                {
                    throw new Exception($"No ShaderFunction parameter type for {param.Usage}");
                }
            }

            // Add the shader function body.
            shaderFunctionBuilder.AddLine(m_functionDescriptor.Body);

            // Return the results of ShaderFoundry's build.
            return shaderFunctionBuilder.Build();
        }

        RegistryKey IRegistryEntry.GetRegistryKey()
        {
            return new RegistryKey
            {
                Name = m_functionDescriptor.Name,
                Version = m_functionDescriptor.Version
            };
        }

        RegistryFlags IRegistryEntry.GetRegistryFlags()
        {
            return RegistryFlags.Func;
        }
    }
}