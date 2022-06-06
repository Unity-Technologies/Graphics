using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    public class SubGraphTests : BaseGraphWindowTest
    {
        protected override string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}";
        ModelInspectorView m_InspectorView;

        [SetUp]
        public override void SetUp()
        {
            CreateWindow();

            m_GraphView = m_Window.GraphView as TestGraphView;

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateSubGraphAssetAction>();
            newGraphAction.Action(0, testAssetPath, "");
            var graphAsset = ShaderGraphAssetUtils.HandleLoad(testAssetPath);
            m_Window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
            m_Window.GraphTool.Update();

            m_Window.Focus();

            FindInspectorView();
        }

        private void FindInspectorView()
        {
            const string viewFieldName = "m_InspectorView";

            var found = m_Window.TryGetOverlay(k_InspectorOverlayId, out var inspectorOverlay);
            Assert.IsTrue(found, "Inspector overlay was not found");

            m_InspectorView = (ModelInspectorView)inspectorOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(inspectorOverlay);
            Assert.IsNotNull(m_InspectorView, "Inspector view was not found");
        }

        [Test]
        public void TestGraphModelIsSubGraph()
        {
            var model = (ShaderGraphModel)m_Window.GraphView.GraphModel;
            Assert.IsTrue(model.IsSubGraph, "GraphModel.IsSubGraph should be true for subgraph asset");
        }

        [UnityTest]
        public IEnumerator TestCanAddNodeToSubGraph()
        {
            return AddNodeFromSearcherAndValidate("Add");
        }

        [UnityTest]
        public IEnumerator TestSaveSubGraph()
        {
            yield return AddNodeFromSearcherAndValidate("Add");
            yield return SaveAndReopenGraph();

            // Wait till the graph model is loaded back up
            while (m_Window.GraphView.GraphModel == null)
                yield return null;

            Assert.IsTrue(FindNodeOnGraphByName("Add"));
        }

        [UnityTest]
        public IEnumerator TestSubgraphOutputHasEditor()
        {
            const string outputNodeName = "DefaultContextDescriptor";
            const string outputInspectorListName = "sg-subgraph-output-list";

            var output = GetNodeModelFromGraphByName(outputNodeName);
            Assert.IsNotNull(output);

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, output));
            yield return null;

            var list = m_InspectorView.Q<ListView>(outputInspectorListName);
            Assert.IsNotNull(list, "Subgraph output node should display a list in the inspector");
        }
    }
}
