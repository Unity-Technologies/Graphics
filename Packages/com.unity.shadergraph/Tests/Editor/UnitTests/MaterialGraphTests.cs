using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class MaterialGraphTests
    {
        private class TestNode : AbstractMaterialNode
        { }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCreateMaterialGraph()
        {
            var graph = new GraphData();

            Assert.IsNotNull(graph);

            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestUndoRedoPerformedMethod()
        {
            var view = new MaterialGraphView();
            var viewType = typeof(MaterialGraphView);
            var fieldInfo = viewType.GetField("m_UndoRedoPerformedMethodInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfoValue = fieldInfo.GetValue(view);

            Assert.IsNotNull(fieldInfoValue, "m_UndoRedoPerformedMethodInfo must not be null.");
        }

        private const string kTempAssetPath = "Packages/com.unity.shadergraph/Tests/Editor/TempGraphKeepUnsavedChange.shadergraph";

        [UnityTest]
        public IEnumerator TestMaterialGraphKeepUnsavedChange()
        {
            if (Display.main == null || Application.isBatchMode)
            {
                yield break;
            }

            var graphData = new GraphData();
            graphData.AddContexts();
            graphData.InitializeOutputs(null, null);
            graphData.AddCategory(CategoryData.DefaultCategory());
            graphData.path = "TempGraph";

            FileUtilities.WriteShaderGraphToDisk(kTempAssetPath, graphData);
            AssetDatabase.Refresh();

            string assetGuid = AssetDatabase.AssetPathToGUID(kTempAssetPath);

            var window = EditorWindow.CreateWindow<MaterialGraphEditWindow>();
            window.Initialize(assetGuid);
            window.Focus();

            // Make an unsaved change that should be kept on Domain Reload.
            window.graphObject.graph.AddNode(new TestNode());

            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            window = EditorWindow.GetWindow<MaterialGraphEditWindow>();
            graphData = window?.graphObject?.graph;

            // Assert later to always cleanup window and asset.
            bool windowNotLost = window != null;
            bool keepChange = graphData != null && graphData.GetNodes<TestNode>().Any();

            if (windowNotLost)
            {
                window.graphObject = null; // Prevent a save prompt from popping.
                window.Close();
            }
            AssetDatabase.DeleteAsset(kTempAssetPath);
            AssetDatabase.Refresh();

            Assert.IsTrue(windowNotLost);
            Assert.IsTrue(keepChange);
        }
    }
}
