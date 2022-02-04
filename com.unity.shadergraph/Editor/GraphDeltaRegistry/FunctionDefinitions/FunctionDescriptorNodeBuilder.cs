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
            // TODO (Brett) This is incorrect
            //portWriter.SetField<int>(GraphType.kLength, 1);

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

            //port.SetField<GraphType.Primitive>(GraphType.kPrecision, m_functionDescriptor.param1.precision);
            //port.SetField<GraphType.Precision>(GraphType.kPrimitive, m_functionDescriptor.param1.primitive);
            //port.SetField(GraphType.kHeight, 1);
            //port.SetField(GraphType.kLength, 1);
        }

        //void INodeDefinitionBuilder.BuildNode(
        //    INodeReader userData,
        //    INodeWriter generatedData,
        //    Registry registry)
        //{
        //    throw new System.NotImplementedException();
        //}

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(
            INodeReader data,
            ShaderContainer container,
            Registry registry)
        {
            // TODO (Brett) Implement
            throw new System.NotImplementedException();
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