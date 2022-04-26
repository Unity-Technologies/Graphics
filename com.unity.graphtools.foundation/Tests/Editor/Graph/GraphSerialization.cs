using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Graph
{
    class TestGraphTemplate : IGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        /// <inheritdoc />
        public string GraphFileExtension => "asset";

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
            GraphAssetCreationHelpers<TestGraphAsset>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            AssumeIntegrity();

            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(GraphTool.ToolState.CurrentGraph.GetGraphAsset() as Object);
            var asset = OpenedGraph.Load(k_GraphPath, 0);
            GraphTool.Dispatch(new LoadGraphCommand(asset.GraphModel));
            Assert.AreEqual(k_GraphPath, AssetDatabase.GetAssetPath((Object)GraphTool.ToolState.CurrentGraph.GetGraphAsset()));
            Assert.AreEqual(GraphTool.ToolState.GraphModel, GraphModel);
            AssertIntegrity();
        }

        [Test]
        public void CreateGraphAssetBuildsValidGraphModel()
        {
            GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(ClassStencil), "test");
            AssumeIntegrity();
        }

        [Test]
        public void CreateGraphAssetWithTemplateBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraphTemplate();
            GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, graphTemplate);
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            var graphTemplate = new TestGraphTemplate();
            GraphAssetCreationHelpers<TestGraphAsset>.CreateGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate);

            var graphAsset = AssetDatabase.LoadAssetAtPath<GraphAsset>(k_GraphPath);
            Resources.UnloadAsset(graphAsset);
            var asset = OpenedGraph.Load(k_GraphPath, 0);
            GraphTool.Dispatch(new LoadGraphCommand(asset.GraphModel));

            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphFromAsset()
        {
            var graphTemplate = new TestGraphTemplate();
            var asset = ScriptableObject.CreateInstance<TestGraphAsset>();
            var doCreateAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            doCreateAction.SetUp(null, asset, graphTemplate);
            doCreateAction.CreateAndLoadAsset(k_GraphPath);
            AssertIntegrity();
        }

        [Test]
        public void GetAssetPathOnSubAssetDoesNotLoadMainAsset()
        {
            // Create asset file with two graph assets in it.
            GraphAssetCreationHelpers<ClassGraphAsset>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            var subAsset = GraphAssetCreationHelpers<OtherClassGraphAsset>.CreateGraphAsset(typeof(ClassStencil), "otherTest", null);
            AssetDatabase.AddObjectToAsset(subAsset, k_GraphPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subAsset, out var guid, out long localId);
            Resources.UnloadAsset(GraphTool.ToolState.CurrentGraph.GetGraphAsset() as Object);

            // Load the second asset.
            var asset = OpenedGraph.Load(k_GraphPath, localId);
            GraphTool.Dispatch(new LoadGraphCommand(asset.GraphModel));

            // Check that we loaded the second asset.
            Assert.AreEqual(guid, GraphTool.ToolState.CurrentGraph.GraphAssetGuid);
            Assert.AreEqual(localId, GraphTool.ToolState.CurrentGraph.AssetLocalId);

            // Call GetGraphAssetPath(), which was reloading the wrong asset (GTF-350).
            GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();
            Assert.AreEqual(subAsset, GraphTool.ToolState.CurrentGraph.GetGraphAssetWithoutLoading());

            // Call GetGraphAssetPath(), which was reloading the wrong asset (GTF-350).
            GraphTool.ToolState.CurrentGraph.GetGraphAsset();
            Assert.AreEqual(subAsset, GraphTool.ToolState.CurrentGraph.GetGraphAssetWithoutLoading());
        }

        class ObjectA : ScriptableObject { }

        static IGraphAsset CreateGraph<TGraphAsset, TStencil>() where TGraphAsset : GraphAsset where TStencil : Stencil
        {
            var graphAsset = ScriptableObject.CreateInstance<TGraphAsset>() as IGraphAsset;

            if (graphAsset as Object != null)
            {
                var template = new TestGraphTemplate();
                graphAsset.CreateGraph(typeof(TStencil));
                template.InitBasicGraph(graphAsset.GraphModel);
            }

            return graphAsset;
        }

        [Test]
        public void CanLoadGraphThatIsNotTheMainAsset()
        {
            var obj = ScriptableObject.CreateInstance<ObjectA>();
            var graph = CreateGraph<ClassGraphAsset, ClassStencil>();
            AssetDatabase.CreateAsset(obj, k_GraphPath);
            AssetDatabase.AddObjectToAsset(graph as Object, k_GraphPath);
            AssetDatabase.SaveAssets();

            var loadedGraph = AssetDatabase.LoadAssetAtPath<ClassGraphAsset>(k_GraphPath);
            GraphTool.Dispatch(new LoadGraphCommand(loadedGraph.GraphModel));
            Assert.AreEqual(loadedGraph, GraphTool.ToolState.CurrentGraph.GetGraphAssetWithoutLoading());
        }
    }
}
