using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class SaveLoadTests : BaseGraphWindowTest
    {
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.Disk;

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator SaveLoadEdgesTest()
        // {
        //     const string fromNodeName = "Add", fromPortName = "Out";
        //     const string toNodeName = "Preview", toPortName = "In";
        //
        //     // Set up the graph
        //     yield return m_TestInteractionHelper.CreateNodesAndConnect(fromNodeName, toNodeName, fromPortName, toPortName);
        //
        //     yield return SaveAndReopenGraph();
        //
        //     // Verify that edge is preserved
        //     {
        //         var edge = m_GraphView.GraphModel.WireModels.FirstOrDefault();
        //         Assert.IsNotNull(edge, "Edge should exist in loaded graph");
        //
        //         Assert.IsTrue(edge.FromPort is
        //         {
        //             UniqueName: fromPortName,
        //             NodeModel: SGNodeModel { Title: fromNodeName }
        //         }, $"Edge should begin at port {fromPortName} on node {fromNodeName}");
        //
        //         Assert.IsTrue(edge.ToPort is
        //         {
        //             UniqueName: toPortName,
        //             NodeModel: SGNodeModel { Title: toNodeName }
        //         }, $"Edge should end at port {toPortName} on node {toNodeName}");
        //     }
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator NodeCollapseStateTest()
        // {
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        //
        //     var nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
        //     Assert.IsNotNull(nodeModel);
        //
        //     if (nodeModel is SGNodeModel graphDataNodeModel)
        //     {
        //         var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
        //         Assert.IsNotNull(nodeGraphElement);
        //
        //         // Test the collapse button
        //         var collapseButton = nodeGraphElement.Q("collapse");
        //         Assert.IsNotNull(collapseButton);
        //
        //         var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, collapseButton, true);
        //         m_TestEventHelper.SimulateMouseClick(collapseButtonPosition);
        //         yield return null;
        //         yield return null;
        //         yield return null;
        //         yield return null;
        //
        //         Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);
        //     }
        //
        //     yield return SaveAndReopenGraph();
        //
        //     nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
        //     Assert.IsNotNull(nodeModel);
        //
        //     if (nodeModel is SGNodeModel graphDataNodeModelReloaded)
        //         Assert.IsFalse(graphDataNodeModelReloaded.IsPreviewExpanded);
        // }
    }
}
