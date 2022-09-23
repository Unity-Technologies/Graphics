using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class GraphEdgeTests : BaseGraphWindowTest
    {
        [UnityTest]
        public IEnumerator TestSaveLoadEdges()
        {
            const string fromNodeName = "Add", fromPortName = "Out";
            const string toNodeName = "Preview", toPortName = "In";

            // Set up the graph
            yield return m_TestInteractionHelper.CreateNodesAndConnect(fromNodeName, toNodeName, fromPortName, toPortName);

            yield return SaveAndReopenGraph();

            // Verify that edge is preserved
            {
                var edge = m_GraphView.GraphModel.WireModels.FirstOrDefault();
                Assert.IsNotNull(edge, "Edge should exist in loaded graph");

                Assert.IsTrue(edge.FromPort is
                {
                    UniqueName: fromPortName,
                    NodeModel: GraphDataNodeModel { Title: fromNodeName }
                }, $"Edge should begin at port {fromPortName} on node {fromNodeName}");

                Assert.IsTrue(edge.ToPort is
                {
                    UniqueName: toPortName,
                    NodeModel: GraphDataNodeModel { Title: toNodeName }
                }, $"Edge should end at port {toPortName} on node {toNodeName}");
            }
        }

        [UnityTest]
        public IEnumerator TestEdgeCanBeDeleted()
        {
            // Set up the graph
            yield return m_TestInteractionHelper.CreateNodesAndConnect();

            var edgeModel = m_Window.GetEdgeModelFromGraphByName("Add", "Preview");

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edgeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            edgeModel = m_Window.GetEdgeModelFromGraphByName("Add", "Preview");
            Assert.IsNull(edgeModel, "Edge should be null after delete operation");
        }

        [UnityTest]
        public IEnumerator TestEdgeCanBeCopied()
        {
            // Set up the graph
            yield return m_TestInteractionHelper.CreateNodesAndConnect();

            var modelsToCopy = m_GraphView.GraphModel.NodeModels.Where(model => model is not GraphDataContextNodeModel);

            yield return m_TestInteractionHelper.SelectAndCopyNodes(modelsToCopy.ToList());

            var edgeModels = m_Window.GetEdgeModelsFromGraphByName("Add", "Preview");
            Assert.IsTrue(edgeModels.Count == 2);
        }
    }
}
