using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests.DataModel
{
    class SGNodeModelTest
    {
        #region Test Nodes

        // Empty node: no inputs, no outputs, no body.
        static readonly RegistryKey k_EmptyNodeKey = new() {Name = "TestEmpty", Version = 1};
        static readonly FunctionDescriptor k_EmptyNodeDescriptor = new(k_EmptyNodeKey.Name, "", Array.Empty<ParameterDescriptor>());

        // Multi function node: two empty functions, "Main" and "Alternate".
        static readonly RegistryKey k_MultiFunctionKey = new() {Name = "TestMultiFunction", Version = 1};
        const string k_MultiFunctionMainFunc = "Main";
        const string k_MultiFunctionAltFunc = "Alternate";
        static readonly NodeDescriptor k_MultiFunctionDescriptor = new(k_MultiFunctionKey.Version,
            k_MultiFunctionKey.Name,
            k_MultiFunctionMainFunc,
            new FunctionDescriptor(k_MultiFunctionMainFunc, "", Array.Empty<ParameterDescriptor>()),
            new FunctionDescriptor(k_MultiFunctionAltFunc, "", Array.Empty<ParameterDescriptor>())
        );

        // TODO: Options node: one port with two referable options, OptionA and OptionB.

        #endregion

        SGGraphModel graphModel { get; set; }

        [SetUp]
        public void SetUp()
        {
            var registry = new ShaderGraphRegistry();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<GradientType>();
            registry.Register<GradientTypeAssignment>();
            registry.Register<BaseTextureType>();
            registry.Register<BaseTextureTypeAssignment>();
            registry.Register<SamplerStateType>();
            registry.Register<SamplerStateAssignment>();

            registry.Register(k_EmptyNodeDescriptor, 1);
            registry.Register(k_MultiFunctionDescriptor);

            graphModel = SGGraphModelMock.CreateWithGraphHandler(registry);
        }

        (NodeHandler, SGNodeModel) MakeNode(RegistryKey? key = null)
        {
            var nodeHandler = graphModel.GraphHandler.AddNode(key ?? k_EmptyNodeKey, "Test");
            var node = graphModel.CreateNode<SGNodeModel>("Test", initializationCallback: nm => nm.graphDataName = "Test");
            return (nodeHandler, node);
        }

        SGNodeModel MakeSearcherPreviewNode(RegistryKey? key = null)
        {
            return graphModel.CreateNode<SGNodeModel>("Test", spawnFlags: SpawnFlags.Orphan, initializationCallback: nm => nm.SetSearcherPreviewRegistryKey(key ?? k_EmptyNodeKey));
        }

        SGNodeModel MakeNodeWithoutBackingData()
        {
            // OnDefineNode will log an error because the created node doesn't have a valid backing
            var ignoreFailing = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            var node = graphModel.CreateNode<SGNodeModel>("Test");

            LogAssert.ignoreFailingMessages = ignoreFailing;

            return node;
        }

        [Test]
        public void TestExistsInGraphData_NodeOnGraph_IsTrue()
        {
            var (_, nodeModel) = MakeNode();
            Assert.IsTrue(nodeModel.existsInGraphData);
        }
    }
}
