using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    static class ConstantTestUtils
    {
        // NOTE: Default value `value` is only applicable if supported by the type. See NodeBuilderUtils.ParameterDescriptorToField.
        public static (NodeHandler, PortHandler) MakeTestField(SGGraphModel model, ITypeDescriptor type, object value = null, string name = "TestParam")
        {
            // Arbitrary node to hold our test field
            var nodeHandler = model.GraphHandler.AddNode(new RegistryKey {Name = "Add", Version = 1}, $"Test_{name}");

            var param = new ParameterDescriptor(name, type, GraphType.Usage.In, value);
            var portHandler = NodeBuilderUtils.ParameterDescriptorToField(param, TYPE.Any, nodeHandler, model.GraphHandler.registry);

            return (nodeHandler, portHandler);
        }
    }
}
