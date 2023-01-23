using System;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests.DataModel
{
    class SGNodeModelTest
    {
        #region Test Nodes

        // Empty node: no inputs, no outputs, no body.
        static readonly RegistryKey k_EmptyNodeKey = new() {Name = "TestEmpty", Version = 1};
        static readonly FunctionDescriptor k_EmptyNodeDescriptor = new(k_EmptyNodeKey.Name, "", Array.Empty<ParameterDescriptor>());

        #endregion

        SGGraphModel graphModel { get; set; }

        [SetUp]
        public void SetUp()
        {
            var registry = new ShaderGraphRegistry();
            registry.Register(k_EmptyNodeDescriptor, 1);

            graphModel = SGGraphModelMock.CreateWithGraphHandler(registry);
        }

        (NodeHandler, SGNodeModel) MakeNode(RegistryKey? key = null)
        {
            var nodeHandler = graphModel.GraphHandler.AddNode(key ?? k_EmptyNodeKey, "Test");
            var node = graphModel.CreateNode<SGNodeModel>("Test", initializationCallback: nm => nm.graphDataName = "Test");
            return (nodeHandler, node);
        }

        [Test]
        public void TestExistsInGraphData_NodeOnGraph_IsTrue()
        {
            var (_, nodeModel) = MakeNode();
            Assert.IsTrue(nodeModel.existsInGraphData);
        }
    }
}
