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
        [SetUp]
        public override void SetUp()
        {
            m_TestAssetPath = $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.SubGraphExtension}";

            CreateWindow();

            m_GraphView = m_Window.GraphView as ShaderGraphView;

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateSubGraphAssetAction>();
            newGraphAction.Action(0, m_TestAssetPath, "");
            var graphAsset = ShaderSubGraphAsset.HandleLoad(m_TestAssetPath);
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

            GraphAssetUtils.SaveOpenGraphAsset(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderSubGraphAsset.HandleLoad(m_TestAssetPath);
            CreateWindow();
            m_Window.Show();
            m_Window.Focus();
            m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            yield return null;

            Assert.IsTrue(FindNodeOnGraphByName("Add"));
        }

        [UnityTest]
        public IEnumerator TestSubGraphContainsOutputContext()
        {
            Assert.IsTrue(false); // TODO
            yield return null;
        }
    }
}
