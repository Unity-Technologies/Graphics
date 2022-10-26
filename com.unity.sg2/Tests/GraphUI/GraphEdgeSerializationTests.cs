using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class GraphSerializationTests : BaseGraphWindowTest
    {
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.Disk;

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
        public IEnumerator NodeCollapseStateSerializationTest()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModel)
            {
                var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
                Assert.IsNotNull(nodeGraphElement);

                // Test the collapse button
                var collapseButton = nodeGraphElement.Q("collapse");
                Assert.IsNotNull(collapseButton);

                var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, collapseButton, true);
                m_TestEventHelper.SimulateMouseClick(collapseButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);
            }

            yield return SaveAndReopenGraph();

            nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModelReloaded)
                Assert.IsFalse(graphDataNodeModelReloaded.IsPreviewExpanded);
        }
    }
}
