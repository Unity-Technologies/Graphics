using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Graph
{
    class TestGraph : IGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        public void InitBasicGraph(IGraphModel graph)
        {
            AssetDatabase.SaveAssets();
        }
    }

    class GraphSerialization : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void LoadGraphCommandLoadsCorrectGraph()
        {
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            AssumeIntegrity();

            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(GraphTool.ToolState.AssetModel as Object);
            GraphTool.Dispatch(new LoadGraphAssetCommand(k_GraphPath, 0));
            Assert.AreEqual(k_GraphPath, AssetDatabase.GetAssetPath((Object)GraphModel.AssetModel));
            AssertIntegrity();

            AssetDatabase.DeleteAsset(k_GraphPath);
        }

        [Test]
        public void CreateGraphAssetBuildsValidGraphModel()
        {
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "test");
            AssumeIntegrity();
        }

        [Test]
        public void CreateGraphAssetWithTemplateBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraph();
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, graphTemplate);
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            var graphTemplate = new TestGraph();
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate);

            GraphModel graph = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(k_GraphPath)?.GraphModel as GraphModel;
            Resources.UnloadAsset((Object)graph?.AssetModel);
            GraphTool.Dispatch(new LoadGraphAssetCommand(k_GraphPath, 0));

            AssertIntegrity();

            AssetDatabase.DeleteAsset(k_GraphPath);
        }

        [Test]
        public void CreateTestGraphFromAssetModel()
        {
            var graphTemplate = new TestGraph();
            var assetModel = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            var doCreateAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            doCreateAction.SetUp(null, assetModel, graphTemplate);
            doCreateAction.CreateAndLoadAsset(k_GraphPath);
            AssertIntegrity();
        }

        [Test]
        public void GetAssetPathOnSubAssetDoesNotLoadMainAsset()
        {
            // Create asset file with two graph assets in it.
            GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            var subAsset = GraphAssetCreationHelpers<OtherClassGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "otherTest", null);
            AssetDatabase.AddObjectToAsset(subAsset as Object, k_GraphPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subAsset as Object, out var guid, out long localId);
            Resources.UnloadAsset(GraphTool.ToolState.AssetModel as Object);

            // Load the second asset.
            GraphTool.Dispatch(new LoadGraphAssetCommand(k_GraphPath, localId));

            // Check that we loaded the second asset.
            Assert.AreEqual(guid, GraphTool.ToolState.CurrentGraph.GraphModelAssetGuid);
            Assert.AreEqual(localId, GraphTool.ToolState.CurrentGraph.AssetLocalId);

            // Call GetGraphAssetModelPath(), which was reloading the wrong asset (GTF-350).
            GraphTool.ToolState.CurrentGraph.GetGraphAssetModelPath();
            Assert.AreEqual(subAsset, GraphTool.ToolState.CurrentGraph.GetGraphAssetModelWithoutLoading());

            // Call GetGraphAssetModel(), which was reloading the wrong asset (GTF-350).
            GraphTool.ToolState.CurrentGraph.GetGraphAssetModel();
            Assert.AreEqual(subAsset, GraphTool.ToolState.CurrentGraph.GetGraphAssetModelWithoutLoading());

            AssetDatabase.DeleteAsset(k_GraphPath);
        }
    }
}
