namespace com.unity.shadergraph.defs {
    public class FunctionDescriptorNodeBuilder : Defs.INodeDefinitionBuilder
    {
        private FunctionDescriptor m_functionDescriptor;

        public static IPortWriter ParameterDescriptorToField(
            ParameterDescriptor param,
            INodeReader nodeReader,
            INodeWriter nodeWriter
            )
        {
            IPortWriter port = nodeWriter.AddPort<GraphType>(
                nodeReader,
                param.Name,
                param.Usage == Usage.Input,
                Registry
            );
            // TODO (Brett) This is incorrect
            portWriter.SetField<int>(GraphType.kLength, 1);
        }

        internal FunctionDescriptorNodeBuilder(FunctionDescriptor fd)
        {
            funcDescription = fd; // copy
        }

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public RegistryKey GetRegistryKey() => new RegistryKey
        {
            Name = funcDescription.Name,
            Version = funcDescription.Version
        };

        public void BuildNode(
            INodeReader userData,
            INodeWriter generatedData,
            Registry registry)
        {

            foreach (var param in funcDescription.Params)
            {
                var port = generatedData.AddPort<GraphType>(
                    userData,
                    funcDescription.param1.name,
                    funcDescription.param1.Usage == Input,
                    registry);
                FunctionDescriptorNodeBuilder.ParameterToField(param, userData, generatedData);
            }

            port.SetField<GraphType.Primitive>(GraphType.kPrecision, funcDescription.param1.precision);
            port.SetField<GraphType.Precision>(GraphType.kPrimitive, funcDescription.param1.primitive);
            port.SetField(GraphType.kHeight, 1);
            port.SetField(GraphType.kLength, 1);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(
            INodeReader data,
            ShaderFoundry.ShaderContainer container,
            Registry registry)
        {
            // TODO (Brett) Implement
        }
    }
}