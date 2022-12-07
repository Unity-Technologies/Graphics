using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class SubGraphTests : BaseGraphWindowTest
    {
        protected override string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}";
        ModelInspectorView m_InspectorView;

        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemorySubGraph;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            FindInspectorView();
        }

        private void FindInspectorView()
        {
            const string viewFieldName = "m_InspectorView";

            var found = m_MainWindow.TryGetOverlay(k_InspectorOverlayId, out var inspectorOverlay);
            Assert.IsTrue(found, "Inspector overlay was not found");

            m_InspectorView = (ModelInspectorView)inspectorOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(inspectorOverlay);
            Assert.IsNotNull(m_InspectorView, "Inspector view was not found");
        }

        [Test]
        public void TestGraphModelIsSubGraph()
        {
            var model = (SGGraphModel)m_MainWindow.GraphView.GraphModel;
            Assert.IsTrue(model.IsSubGraph, "GraphModel.IsSubGraph should be true for subgraph asset");
        }

        [UnityTest]
        public IEnumerator TestCanAddNodeToSubGraph()
        {
            return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        }

        [UnityTest]
        public IEnumerator TestSubgraphOutputHasEditor()
        {
            const string outputNodeName = "DefaultContextDescriptor";
            const string outputInspectorListName = "sg-subgraph-output-list";

            var output = m_MainWindow.GetNodeModelFromGraphByName(outputNodeName);
            Assert.IsNotNull(output);

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, output));
            yield return null;

            var list = m_InspectorView.Q<ListView>(outputInspectorListName);
            Assert.IsNotNull(list, "Subgraph output node should display a list in the inspector");
        }
    }
}
