using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    static class ConstantTestUtils
    {
        public static (NodeHandler, PortHandler) MakeTestField(SGGraphModel model, ITypeDescriptor type, object value = null, string name = "TestParam")
        {
            var node = model.CreateGraphDataNode(new RegistryKey {Name = "Add", Version = 1});
            node.TryGetNodeHandler(out var nodeHandler);

            var param = new ParameterDescriptor(name, type, GraphType.Usage.In, value);
            var portHandler = NodeBuilderUtils.ParameterDescriptorToField(param, TYPE.Any, nodeHandler, model.GraphHandler.registry);

            return (nodeHandler, portHandler);
        }
    }
}
