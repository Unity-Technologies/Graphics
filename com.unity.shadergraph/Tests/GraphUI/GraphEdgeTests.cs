using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    public class GraphEdgeTests : BaseGraphWindowTest
    {
        [UnityTest]
        public IEnumerator TestSaveLoadEdges()
        {
            const string fromNodeName = "Add", fromPortName = "Out";
            const string toNodeName = "Preview", toPortName = "In";

            // Set up the graph
            {
                yield return AddNodeFromSearcherAndValidate(fromNodeName);
                yield return AddNodeFromSearcherAndValidate(toNodeName);

                var nodeModels = m_GraphView.GraphModel.NodeModels;
                var addNode = (GraphDataNodeModel)nodeModels.First(n => n is GraphDataNodeModel {Title: fromNodeName});
                var addOut = addNode.GetOutputPorts().First(p => p.UniqueName == fromPortName);

                var previewNode = (GraphDataNodeModel)nodeModels.First(n => n is GraphDataNodeModel {Title: toNodeName});
                var previewIn = previewNode.GetInputPorts().First(p => p.UniqueName == toPortName);

                m_GraphView.Dispatch(new CreateEdgeCommand(previewIn, addOut));
            }

            GraphAssetUtils.SaveImplementation(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderGraphAsset.HandleLoad(m_TestAssetPath);
            CreateWindow();
            m_Window.Show();
            m_Window.Focus();
            m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            yield return null;

            // Verify that edge is preserved
            {
                var edge = m_GraphView.GraphModel.EdgeModels.FirstOrDefault();
                Assert.IsNotNull(edge, "Edge should exist in loaded graph");

                Assert.IsTrue(edge.FromPort is
                {
                    UniqueName: fromPortName,
                    NodeModel: GraphDataNodeModel {Title: fromNodeName}
                }, $"Edge should begin at port {fromPortName} on node {fromNodeName}");

                Assert.IsTrue(edge.ToPort is
                {
                    UniqueName: toPortName,
                    NodeModel: GraphDataNodeModel {Title: toNodeName}
                }, $"Edge should end at port {toPortName} on node {toNodeName}");
            }
        }
    }
}
