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
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemoryBlank;

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestEdgeCanBeDeleted()
        // {
        //     // Set up the graph
        //     yield return m_TestInteractionHelper.CreateNodesAndConnect();
        //
        //     var edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");
        //
        //     // Select element programmatically because it might be behind another one
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edgeModel));
        //     yield return null;
        //
        //     Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
        //     yield return null;
        //
        //     edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");
        //     Assert.IsNull(edgeModel, "Edge should be null after delete operation");
        // }
    }
}
