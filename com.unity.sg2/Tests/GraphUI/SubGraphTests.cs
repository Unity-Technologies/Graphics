using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    public class SubGraphTests : BaseGraphWindowTest
    {
        protected override string testAssetPath =>  $"Assets\\{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}";

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
    }
}
