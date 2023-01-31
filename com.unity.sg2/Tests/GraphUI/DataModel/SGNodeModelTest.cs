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
        #region Test Definitions

        // Empty node: no inputs, no outputs, no body.
        static readonly RegistryKey k_EmptyNodeKey = new() {Name = "TestEmpty", Version = 1};
        static readonly FunctionDescriptor k_EmptyNodeDescriptor = new(k_EmptyNodeKey.Name, "", Array.Empty<ParameterDescriptor>());

        // Multi function node: two empty functions, "Main" and "Alternate".
        static readonly RegistryKey k_MultiFunctionKey = new() {Name = "TestMultiFunction", Version = 1};
        const string k_MultiFunctionMainFunc = "Main";
        const string k_MultiFunctionAltFunc = "Alternate";
        static readonly NodeDescriptor k_MultiFunctionDescriptor = new(
            k_MultiFunctionKey.Version,
            k_MultiFunctionKey.Name,
            k_MultiFunctionMainFunc,
            new FunctionDescriptor(k_MultiFunctionMainFunc, "", Array.Empty<ParameterDescriptor>()),
            new FunctionDescriptor(k_MultiFunctionAltFunc, "", Array.Empty<ParameterDescriptor>())
        );

        // Options node: two options "OptionA" and "OptionB" on a Vec4 port called In. Also has an input port called
        // In2 with no options to use for comparison. Requires a context node -- see SetUp method.
        static readonly RegistryKey k_OptionsNodeKey = new() {Name = "TestOptions", Version = 1};
        static readonly string k_OptionsNodeValueA = "OptionA";
        static readonly string k_OptionsNodeValueB = "OptionB";
        static readonly NodeDescriptor k_OptionsNodeDescriptor = new(
            k_OptionsNodeKey.Version,
            k_OptionsNodeKey.Name,
            "Main",
            new FunctionDescriptor("Main", "", new ParameterDescriptor[] { new("In", TYPE.Vec4, GraphType.Usage.In, new ReferenceValueDescriptor(k_OptionsNodeValueA)), new("In2", TYPE.Vec4, GraphType.Usage.In) }));
        static readonly NodeUIDescriptor k_OptionsNodeUIDescriptor = new(
            k_OptionsNodeKey.Version,
            k_OptionsNodeKey.Name,
            "Tooltip", "Category", new string[] { }, "Options", parameters: new ParameterUIDescriptor[]
            {
                new ("In", options: new List<(string, object)>
                {
                    (k_OptionsNodeValueA, new ReferenceValueDescriptor(k_OptionsNodeValueA)),
                    (k_OptionsNodeValueB, new ReferenceValueDescriptor(k_OptionsNodeValueB)),
                }),
                new ("In2"),
            });


        // Multi version node: v1 is empty, v2 has a single vec4 input port called In.
        static readonly RegistryKey k_MultiVersionKeyV1 = new() {Name = "MultiVersion", Version = 1};
        static readonly RegistryKey k_MultiVersionKeyV2 = new() {Name = "MultiVersion", Version = 2};
        static readonly FunctionDescriptor k_MultiVersionDescriptorV1 = new(k_MultiVersionKeyV1.Name, "", Array.Empty<ParameterDescriptor>());
        static readonly FunctionDescriptor k_MultiVersionDescriptorV2 = new(k_MultiVersionKeyV2.Name, "", new ParameterDescriptor[] {new("In", TYPE.Vec4, GraphType.Usage.In)});

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
            registry.Register(k_OptionsNodeDescriptor, k_OptionsNodeUIDescriptor);
            registry.Register(k_MultiVersionDescriptorV1, 1);
            registry.Register(k_MultiVersionDescriptorV2, 2);

            graphModel = SGGraphModelMock.CreateWithGraphHandler(registry);

            var optionsCtx = graphModel.GraphHandler.AddContextNode("OptionsNodeContext");
            ContextBuilder.AddContextEntry(optionsCtx, TYPE.Vec4, k_OptionsNodeValueA, graphModel.RegistryInstance.Registry);
            ContextBuilder.AddContextEntry(optionsCtx, TYPE.Vec4, k_OptionsNodeValueB, graphModel.RegistryInstance.Registry);
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
        public void TestTryGetNodeHandler_NodeInSearcher_GetsDefaultTopologyHandler()
        {
            var nodeModel = MakeSearcherPreviewNode();
            Assert.IsTrue(nodeModel.TryGetNodeHandler(out var handler));
            Assert.AreEqual(graphModel.RegistryInstance.DefaultTopologies.graphDelta, handler.Owner);
        }

        [Test]
        public void TestTryGetNodeHandler_NodeOnGraph_GetsHandler()
        {
            var (nodeHandler, nodeModel) = MakeNode();
            Assert.IsTrue(nodeModel.TryGetNodeHandler(out var retrievedHandler));
            Assert.AreEqual(nodeHandler.ID.FullPath, retrievedHandler.ID.FullPath);
        }

        [Test]
        public void TestTryGetNodeHandler_MissingNode_Fails()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.IsFalse(nodeModel.TryGetNodeHandler(out _));
        }

        [Test]
        public void TestExistsInGraphData_NodeInSearcher_IsFalse()
        {
            var nodeModel = MakeSearcherPreviewNode();
            Assert.IsFalse(nodeModel.existsInGraphData);
        }

        [Test]
        public void TestExistsInGraphData_NodeOnGraph_IsTrue()
        {
            var (_, nodeModel) = MakeNode();
            Assert.IsTrue(nodeModel.existsInGraphData);
        }

        [Test]
        public void TestExistsInGraphData_MissingNode_IsFalse()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.IsFalse(nodeModel.existsInGraphData);
        }

        [Test]
        public void TestGetRegistryKey_NodeOnGraph_MatchesHandler()
        {
            var (nodeHandler, nodeModel) = MakeNode();
            Assert.AreEqual(nodeHandler.GetRegistryKey(), nodeModel.registryKey);
        }

        [Test]
        public void TestGetRegistryKey_NodeInSearcher_IsPreviewKey()
        {
            var nodeModel = MakeSearcherPreviewNode(k_EmptyNodeKey);
            Assert.AreEqual(k_EmptyNodeKey, nodeModel.registryKey);
        }

        [Test]
        public void TestGetRegistryKey_MissingNode_IsEmptyKey()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.AreEqual(default(RegistryKey), nodeModel.registryKey);
        }

        [Test]
        public void TestChangeNodeFunction_NodeOnGraph_WithFunctionField_UpdatesFunctionField()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_MultiFunctionKey);

            nodeModel.ChangeNodeFunction(k_MultiFunctionAltFunc);
            Assert.AreEqual(k_MultiFunctionAltFunc, nodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME).GetData());
        }

        [Test]
        public void TestChangeNodeFunction_NodeOnGraph_WithoutFunctionField_LogsError()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_EmptyNodeKey);
            Assert.IsNull(nodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME));

            nodeModel.ChangeNodeFunction("NotValid");
            LogAssert.Expect(LogType.Error, "Unable to update selected function. Node has no selected function field.");
            Assert.IsNull(nodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME));
        }

        [Test]
        public void TestChangeNodeFunction_NodeInSearcher_DoesNothing()
        {
            var nodeModel = MakeSearcherPreviewNode(k_MultiFunctionKey);
            Assert.IsTrue(nodeModel.TryGetNodeHandler(out var previewNodeHandler));
            Assert.AreEqual(k_MultiFunctionMainFunc, previewNodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME).GetData());

            nodeModel.ChangeNodeFunction("NotValid");
            Assert.AreEqual(k_MultiFunctionMainFunc, previewNodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME).GetData());
        }

        [Test]
        public void TestChangeNodeFunction_MissingNode_DoesNothing()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.DoesNotThrow(() => { nodeModel.ChangeNodeFunction("NotValid"); });
        }

        [Test]
        public void TestSetPortOption_NodeOnGraph_UpdatesDefaultConnection()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_OptionsNodeKey);

            var inPort = nodeHandler.GetPort("In");
            Assert.AreEqual("out_" + k_OptionsNodeValueA, inPort.GetFirstConnectedPort().ID.LocalPath);

            nodeModel.SetPortOption("In", 1);
            Assert.AreEqual("out_" + k_OptionsNodeValueB, inPort.GetFirstConnectedPort().ID.LocalPath);
        }

        [Test]
        public void TestSetPortOption_NodeInSearcher_DoesNothing()
        {
            var nodeModel = MakeSearcherPreviewNode(k_OptionsNodeKey);
            Assert.IsTrue(nodeModel.TryGetNodeHandler(out var previewHandler));

            Assert.IsNull(previewHandler.GetPort("In").GetFirstConnectedPort());
            nodeModel.SetPortOption("In", 1);
            Assert.IsNull(previewHandler.GetPort("In").GetFirstConnectedPort());
        }

        [Test]
        public void TestSetPortOption_MissingNode_DoesNothing()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.DoesNotThrow(() => { nodeModel.SetPortOption("NotValid", 0);});
        }

        [Test]
        public void TestGetCurrentPortOption_NodeOnGraph_PortWithoutOptions_IsMinusOne()
        {
            var (_, nodeModel) = MakeNode(k_EmptyNodeKey);

            Assert.AreEqual(-1, nodeModel.GetCurrentPortOption("In2"));
        }

        [Test]
        public void TestGetCurrentPortOption_NodeOnGraph_InvalidPort_IsMinusOne()
        {
            var (_, nodeModel) = MakeNode(k_EmptyNodeKey);

            Assert.AreEqual(-1, nodeModel.GetCurrentPortOption("NotValid"));
        }

        [Test]
        public void TestGetCurrentPortOption_NodeOnGraph_PortWithOptions_MatchesDefaultConnection()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_OptionsNodeKey);

            Assert.AreEqual(0, nodeModel.GetCurrentPortOption("In"));

            var inPort = nodeHandler.GetPort("In");
            inPort.Owner.RemoveDefaultConnection(k_OptionsNodeValueA, inPort.ID, graphModel.RegistryInstance.Registry);
            inPort.Owner.AddDefaultConnection(k_OptionsNodeValueB, inPort.ID, graphModel.RegistryInstance.Registry);

            Assert.AreEqual(1, nodeModel.GetCurrentPortOption("In"));
        }

        [Test]
        public void TestGetCurrentPortOption_NodeInSearcher_IsDefault()
        {
            var nodeModel = MakeSearcherPreviewNode(k_OptionsNodeKey);
            Assert.AreEqual(0, nodeModel.GetCurrentPortOption("In"));
        }

        [Test]
        public void TestGetCurrentPortOption_MissingNode_IsMinusOne()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.AreEqual(-1, nodeModel.GetCurrentPortOption("NotValid"));
        }

        [Test]
        public void TestGetCurrentVersion_NodeOnGraph_MatchesRegistryKey()
        {
            var (_, nodeModel1) = MakeNode(k_MultiVersionKeyV1);
            Assert.AreEqual(k_MultiVersionKeyV1.Version, nodeModel1.currentVersion);

            var (_, nodeModel2) = MakeNode(k_MultiVersionKeyV2);
            Assert.AreEqual(k_MultiVersionKeyV2.Version, nodeModel2.currentVersion);
        }

        [Test]
        public void TestGetCurrentVersion_NodeInSearcher_MatchesRegistryKey()
        {
            var nodeModel1 = MakeSearcherPreviewNode(k_MultiVersionKeyV1);
            Assert.AreEqual(k_MultiVersionKeyV1.Version, nodeModel1.currentVersion);

            var nodeModel2 = MakeSearcherPreviewNode(k_MultiVersionKeyV2);
            Assert.AreEqual(k_MultiVersionKeyV2.Version, nodeModel2.currentVersion);
        }

        [Test]
        public void TestGetCurrentVersion_MissingNode_IsZero()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.AreEqual(0, nodeModel.currentVersion);
        }

        [Test]
        public void TestGetLatestAvailableVersion_NodeOnGraph_MatchesRegistry()
        {
            var (_, nodeModel) = MakeNode(k_MultiVersionKeyV1);
            Assert.AreEqual(k_MultiVersionKeyV2.Version, nodeModel.latestAvailableVersion);
        }

        [Test]
        public void TestGetLatestAvailableVersion_NodeInSearcher_MatchesRegistry()
        {
            var nodeModel = MakeSearcherPreviewNode(k_MultiVersionKeyV1);
            Assert.AreEqual(k_MultiVersionKeyV2.Version, nodeModel.latestAvailableVersion);
        }

        [Test]
        public void TestGetLatestAvailableVersion_MissingNode_IsZero()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.AreEqual(0, nodeModel.latestAvailableVersion);
        }

        [Test]
        public void TestUpgradeToLatestVersion_NodeOnGraph_OutOfDate_UpdatesNode()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_MultiVersionKeyV1);
            Assert.AreEqual(k_MultiVersionKeyV1, nodeHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
            Assert.IsEmpty(nodeModel.GetInputPorts());

            nodeModel.UpgradeToLatestVersion();
            Assert.AreEqual(k_MultiVersionKeyV2, nodeHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
            Assert.IsNotEmpty(nodeModel.GetInputPorts());
        }

        [Test]
        public void TestUpgradeToLatestVersion_NodeOnGraph_LatestVersion_DoesNothing()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_MultiVersionKeyV2);
            Assert.AreEqual(k_MultiVersionKeyV2, nodeHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));

            nodeModel.UpgradeToLatestVersion();
            Assert.AreEqual(k_MultiVersionKeyV2, nodeHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
        }

        [Test]
        public void TestUpgradeToLatestVersion_NodeOnGraph_MissingVersion_LogsError()
        {
            var (nodeHandler, nodeModel) = MakeNode(k_MultiVersionKeyV1);

            var fakeKey = new RegistryKey {Name = k_MultiVersionKeyV1.Name, Version = 100};
            nodeHandler.SetMetadata(GraphDelta.GraphDelta.kRegistryKeyName, fakeKey);

            nodeModel.UpgradeToLatestVersion();
            LogAssert.Expect(LogType.Error, $"Node version ({100}) is greater than latest version in registry ({2})");
            Assert.AreEqual(fakeKey, nodeHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
        }

        [Test]
        public void TestUpgradeToLatestVersion_NodeInSearcher_DoesNothing()
        {
            var nodeModel = MakeSearcherPreviewNode(k_MultiVersionKeyV1);
            Assert.IsTrue(nodeModel.TryGetNodeHandler(out var previewHandler));
            Assert.AreEqual(k_MultiVersionKeyV1, previewHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
            Assert.IsEmpty(nodeModel.GetInputPorts());

            nodeModel.UpgradeToLatestVersion();
            Assert.AreEqual(k_MultiVersionKeyV1, previewHandler.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName));
            Assert.IsEmpty(nodeModel.GetInputPorts());
        }

        [Test]
        public void TestUpgradeToLatestVersion_MissingNode_DoesNothing()
        {
            var nodeModel = MakeNodeWithoutBackingData();
            Assert.DoesNotThrow(() => { nodeModel.UpgradeToLatestVersion(); });
        }
    }
}
