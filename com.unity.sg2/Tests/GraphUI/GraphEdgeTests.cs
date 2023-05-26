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
            // create and connect two nodes
            var addNodeModel = SGGraphTestUtils.CreateNodeByName(GraphModel,"Add", Vector2.zero);
            Assert.NotNull(addNodeModel, "Add node could not be added to the graph");
            var previewNodeModel = SGGraphTestUtils.CreateNodeByName(GraphModel, "Preview", Vector2.zero);
            Assert.NotNull(previewNodeModel, "Preview node model could not be added to the graph");

            var edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edgeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            edgeModel = m_MainWindow.GetEdgeModelFromGraphByName("Add", "Preview");
            Assert.IsNull(edgeModel, "Edge should be null after delete operation");
        }
    }
}
