using NUnit.Framework;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests.DataModel.Constants
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

        public static BaseShaderGraphConstant MakeAndBindConstant(SGGraphModel graphModel, TypeHandle typeHandle, NodeHandler nodeHandler, PortHandler portHandler)
        {
            var node = graphModel.CreateConstantNode(typeHandle, "c", Vector2.zero);
            var constant = node.Value as BaseShaderGraphConstant;
            Assert.IsNotNull(constant);

            constant.BindTo(nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);
            return constant;
        }
    }
}
