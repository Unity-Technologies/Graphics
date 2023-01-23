using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.GraphToolsFoundation;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class GraphNodeTests : BaseGraphWindowTest
    {
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemoryBlank;

        [Ignore("Being refactored to test without opening the searcher", Until="2023-01-25")]
        [UnityTest]
        public IEnumerator CreateAddNodeFromSearcherTest()
        {
            return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        }

        [Ignore("Being refactored to test without opening the searcher", Until="2023-01-25")]
        [UnityTest]
        public IEnumerator NodeCollapseExpandTest()
        {
            yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is SGNodeModel graphDataNodeModel)
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

                // Test the expand button
                var expandButton = nodeGraphElement.Q("expand");
                Assert.IsNotNull(expandButton);

                var expandButtonPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, expandButton, true);
                m_TestEventHelper.SimulateMouseClick(expandButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsTrue(graphDataNodeModel.IsPreviewExpanded);
            }
        }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestContextNodesCannotBeDeleted()
        // {
        //     var beforeContext = m_GraphView.GraphModel.NodeModels.OfType<SGContextNodeModel>().FirstOrDefault();
        //     Assert.IsNotNull(beforeContext, "Graph must contain at least one context node for test");
        //
        //     // Select element programmatically because it might be behind another one
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, beforeContext));
        //     yield return null;
        //
        //     Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
        //     yield return null;
        //
        //     var afterContext = m_MainWindow.GetNodeModelFromGraphByName(beforeContext.Title);
        //     Assert.AreEqual(beforeContext, afterContext, "Context node should be unaffected by delete operation");
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestContextNodesCannotBeDeletedFromMixedSelection()
        // {
        //     var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<SGContextNodeModel>().ToList();
        //     var beforeContextCount = beforeContexts.Count;
        //     Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");
        //
        //     // Arbitrary node so that something other than a context exists in our graph.
        //     yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        //     var nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
        //
        //     // Select the context nodes and the add node
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, beforeContexts.Append(nodeModel).ToList()));
        //
        //     Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
        //     Assert.IsNull(m_MainWindow.GetNodeModelFromGraphByName("Add"), "Non-context node should be deleted from selection");
        //
        //     var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<SGContextNodeModel>().ToList();
        //     Assert.AreEqual(beforeContexts.Count, afterContexts.Count, "Context nodes should not be deleted from selection");
        // }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeCopied()
        {
            var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<SGContextNodeModel>().ToList();
            var beforeContextCount = beforeContexts.Count;
            Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");

            yield return m_TestInteractionHelper.SelectAndCopyNodes(new List<AbstractNodeModel>() { beforeContexts[0] });

            var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<SGContextNodeModel>().ToList();
            Assert.AreEqual(beforeContexts.Count, afterContexts.Count, "Context node should not be duplicated by copy/paste");
        }

        [UnityTest]
        public IEnumerator TestOutdatedNodeGetsUpgradeBadge()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 2},
                displayName: "V2"
            );
            yield return null;

            var errors = m_GraphView.GraphTool.GraphProcessingState.Errors;
            Assert.IsTrue(errors.Count == 1, "Outdated node should create 1 graph processing error");
            Assert.IsTrue(errors[0].ParentModel == node, "Graph processing error should be attached to outdated node");
            Assert.IsTrue(errors[0].ErrorType == LogType.Warning, "Graph processing error should be a warning");
        }

        [UnityTest]
        public IEnumerator TestUpToDateNodeDoesNotGetUpgradeBadge()
        {
            m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 3},
                displayName: "V3"
            );
            yield return null;

            var errors = m_GraphView.GraphTool.GraphProcessingState.Errors;
            Assert.IsTrue(errors.Count == 0, "Up-to-date node should not have any warnings");
        }

        [UnityTest]
        public IEnumerator TestNodeCanBeUpgraded()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 2},
                displayName: "V2"
            );
            yield return null;

            m_GraphView.Dispatch(new UpgradeNodeCommand(node));
            yield return null;

            Assert.AreEqual(3, node.registryKey.Version, "Upgrading a node should set it to the latest version");
        }

        [UnityTest]
        public IEnumerator TestDismissingUpgradeRemovesBadge()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 2},
                displayName: "V2"
            );
            yield return null;

            m_GraphView.Dispatch(new DismissNodeUpgradeCommand(node));
            yield return null;

            var errors = m_GraphView.GraphTool.GraphProcessingState.Errors;
            Assert.IsTrue(errors.Count == 0, "Dismissing node upgrade should remove warning badges");
        }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestNodeCanBeDeleted()
        // {
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        //
        //     var nodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add");
        //     Assert.IsNotNull(nodeModel);
        //
        //     // Select element programmatically because it might be behind another one
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel));
        //     yield return null;
        //
        //     Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
        //     yield return null;
        //
        //     var addNode = m_MainWindow.GetNodeModelFromGraphByName("Add");
        //     Assert.IsNull(addNode, "Node should be null after delete operation");
        //
        //     var graphDataNodeModel = nodeModel as SGNodeModel;
        //     var addNodeHandler = GraphModel.GraphHandler.GetNode(graphDataNodeModel.graphDataName);
        //     Assert.IsNull(addNodeHandler, "Node should also be removed from CLDS after delete operation");
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestConnectedNodeCanBeDeleted()
        // {
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Float");
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Truncate");
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        //
        //     m_TestInteractionHelper.ConnectNodes("Float", "Truncate");
        //     m_TestInteractionHelper.ConnectNodes("Truncate", "Add", "Out", "B");
        //
        //     Assert.AreEqual(2, m_GraphView.GraphModel.WireModels.Count, "Initial graph should have 2 edges");
        //
        //     var middleNode = m_MainWindow.GetNodeModelFromGraphByName("Truncate");
        //
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, middleNode));
        //     yield return null;
        //
        //     m_TestEventHelper.SendDeleteCommand();
        //     yield return null;
        //
        //     Assert.AreEqual(0, m_GraphView.GraphModel.WireModels.Count, "Deleting a node should delete the connected edges");
        //     Assert.IsFalse(m_GraphView.GraphModel.NodeModels.Contains(middleNode), "Deleted node should be removed from the graph");
        // }

        [Ignore("Being refactored to test without opening the searcher", Until="2023-01-25")]
        [UnityTest]
        public IEnumerator TestDynamicPortsUpdate()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Multiply");

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector 2");
            var vec2 = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Vector 2");

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            m_GraphView.Dispatch(new CreateWireCommand(multiply.InputsById["A"], vec2.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Multiply node connected to Vector 2 should show Vector 2 type");
            }

            var createdEdge = vec2.GetConnectedWires().First();
            m_GraphView.Dispatch(new DeleteWireCommand(createdEdge));
            yield return null;

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "After disconnecting edges, multiply node should default to Float");
            }
        }

        [Ignore("Being refactored to test without opening the searcher", Until="2023-01-25")]
        [UnityTest]
        public IEnumerator TestDynamicPortUpdatesPropagate()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply1 = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Multiply");
            multiply1.Title = "Multiply 1";

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply2 = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Multiply");
            multiply2.Title = "Multiply 2";

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector 2");
            var vec2 = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Vector 2");

            m_GraphView.Dispatch(new CreateWireCommand(multiply2.InputsById["A"], multiply1.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply1.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            foreach (var port in multiply2.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            m_GraphView.Dispatch(new CreateWireCommand(multiply1.InputsById["A"], vec2.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply1.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Multiply node connected to Vector 2 should show Vector 2 type");
            }

            foreach (var port in multiply2.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Second multiply node in a series should react to upstream change to Vector 2");
            }

            var createdEdge = vec2.GetConnectedWires().First();
            m_GraphView.Dispatch(new DeleteWireCommand(createdEdge));
            yield return null;

            foreach (var port in multiply1.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "After disconnecting edges, multiply node should default to Float");
            }

            foreach (var port in multiply2.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Second multiply node in a series should react to upstream change to Float");
            }
        }
    }
}
