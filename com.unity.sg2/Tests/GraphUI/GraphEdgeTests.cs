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

        [UnityTest]
        public IEnumerator TestEdgeCanBeDeleted()
        {
            // Set up the graph
            yield return m_TestInteractionHelper.CreateNodesAndConnect();

            var edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edgeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");
            Assert.IsNull(edgeModel, "Edge should be null after delete operation");
        }

        [UnityTest]
        public IEnumerator TestEdgeCanBeCopied()
        {
            // Set up the graph
            yield return m_TestInteractionHelper.CreateNodesAndConnect();

            var modelsToCopy = m_GraphView.GraphModel.NodeModels.Where(model => model is not GraphDataContextNodeModel);

            yield return m_TestInteractionHelper.SelectAndCopyNodes(modelsToCopy.ToList());

            var edgeModels = m_MainWindow.GetEdgeModelsFromGraphByName("Add", "Preview");
            Assert.IsTrue(edgeModels.Count == 2);
        }
    }
}
