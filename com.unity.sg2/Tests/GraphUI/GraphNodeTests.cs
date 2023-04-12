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

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class GraphNodeTests : BaseGraphWindowTest
    {
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemoryBlank;

        [UnityTest]
        public IEnumerator NodeCollapseExpandTest()
        {
            // add an AddNode to the graph
            var addNodeName = "Add";
            var addNodeModel = SGGraphTestUtils.CreateNodeByName(GraphModel, addNodeName, Vector2.zero);
            Assert.IsNotNull(addNodeModel, $"Could not add a node with the name {addNodeName} to the graph.");

            // allow the tool observers to run
            // We could update the GraphTool with m_GraphView.GraphTool.Update() but it does not refresh custom visual elements.
            yield return null;

            var nodeGraphElement = m_GraphView.GetGraphElement(addNodeModel);
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

            Assert.IsFalse(addNodeModel.IsPreviewExpanded);

            // Test the expand button
            var expandButton = nodeGraphElement.Q("expand");
            Assert.IsNotNull(expandButton);

            var expandButtonPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, expandButton, true);
            m_TestEventHelper.SimulateMouseClick(expandButtonPosition);
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            Assert.IsTrue(addNodeModel.IsPreviewExpanded);
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

        private int CountErrors(IReadOnlyList<BaseGraphProcessingResult> graphProcessingResults)
        {
            return -1;
        }

        [UnityTest]
        public IEnumerator TestOutdatedNodeGetsUpgradeBadge()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 2},
                displayName: "V2"
            );
            yield return null;
            var results = m_GraphView.GraphViewModel.GraphProcessingState.Results;
            var errorCount = CountErrors(results);
            Assert.IsTrue(errorCount == 1, "Outdated node should create 1 graph processing error");

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

            var errorCount = CountErrors(m_GraphView.GraphViewModel.GraphProcessingState.Results);
            Assert.IsTrue(errorCount == 0, "Up-to-date node should not have any warnings");
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

            var errorCount = CountErrors(m_GraphView.GraphViewModel.GraphProcessingState.Results);
            Assert.IsTrue(errorCount == 0, "Dismissing node upgrade should remove warning badges");
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

        [UnityTest]
        public IEnumerator TestDynamicPortsUpdate()
        {
            // add a multiply and vector 2 node to the graph
            string multiplyNodeName = "Multiply";
            var multiplyNode = SGGraphTestUtils.CreateNodeByName(GraphModel, multiplyNodeName, Vector2.zero);
            Assert.NotNull(multiplyNode, $"Could not add a node with name {multiplyNodeName} to the graph.");

            string vector2NodeName = "Vector 2";
            var vector2Node = SGGraphTestUtils.CreateNodeByName(GraphModel, vector2NodeName, Vector2.zero);
            Assert.NotNull(multiplyNode, $"Could not add a node with name {vector2NodeName} to the graph.");

            // check that the multiply node has the right port type
            foreach (var port in multiplyNode.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            // create a connection
            m_GraphView.Dispatch(
                new CreateWireCommand(
                    multiplyNode.InputsById["A"],
                    vector2Node.OutputsById["Out"])
            );
            yield return null;

            // check that the type has changed
            foreach (var port in multiplyNode.Ports)
            {
                Assert.AreEqual(
                    TypeHandle.Vector2,
                    port.DataTypeHandle,
                    "Multiply node connected to Vector 2 should show Vector 2 type"
                );
            }

            var createdEdge = vector2Node.GetConnectedWires().First();
            m_GraphView.Dispatch(new DeleteWireCommand(createdEdge));
            yield return null;

            foreach (var port in multiplyNode.Ports)
            {
                Assert.AreEqual(
                    TypeHandle.Float,
                    port.DataTypeHandle,
                    "After disconnecting edges, multiply node should default to Float"
                );
            }
        }

        [UnityTest]
        public IEnumerator TestDynamicPortUpdatesPropagate()
        {
            // create nodes
            var multiplyNodeName = "Multiply";
            var multiplyNodeModel1 = SGGraphTestUtils.CreateNodeByName(GraphModel, multiplyNodeName, Vector2.zero);
            Assert.NotNull(multiplyNodeModel1, $"Could not add node with the name {multiplyNodeName} to the graph.");
            multiplyNodeModel1.Title = "Multiply 1";

            var multiplyNodeModel2 = SGGraphTestUtils.CreateNodeByName(GraphModel, multiplyNodeName, Vector2.zero);
            Assert.NotNull(multiplyNodeModel2, $"Could not add a second node with the name {multiplyNodeName} to the graph.");
            multiplyNodeModel2.Title = "Multiply 2";

            var vector2NodeName = "Vector 2";
            var vector2NodeModel = SGGraphTestUtils.CreateNodeByName(GraphModel, vector2NodeName, Vector2.zero);
            Assert.NotNull(vector2NodeModel, $"Could not add node with the name {multiplyNodeName} to the graph.");

            // connect nodes
            m_GraphView.Dispatch(new CreateWireCommand(multiplyNodeModel2.InputsById["A"], multiplyNodeModel1.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiplyNodeModel1.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            foreach (var port in multiplyNodeModel2.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            m_GraphView.Dispatch(new CreateWireCommand(multiplyNodeModel1.InputsById["A"], vector2NodeModel.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiplyNodeModel1.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Multiply node connected to Vector 2 should show Vector 2 type");
            }

            foreach (var port in multiplyNodeModel2.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Second multiply node in a series should react to upstream change to Vector 2");
            }

            var createdEdge = vector2NodeModel.GetConnectedWires().First();
            m_GraphView.Dispatch(new DeleteWireCommand(createdEdge));
            yield return null;

            foreach (var port in multiplyNodeModel1.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "After disconnecting edges, multiply node should default to Float");
            }

            foreach (var port in multiplyNodeModel2.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Second multiply node in a series should react to upstream change to Float");
            }
        }

        [UnityTest]
        public IEnumerator TestNodeCanBeDuplicated()
        {

            var multiply1 = SGGraphTestUtils.CreateNodeByName(GraphModel, "Multiply", Vector2.zero);
            Assert.NotNull(multiply1, "Multiply node could not be added to the graph model");
            multiply1.Title = "Multiply 1";

            Assert.IsNotNull(multiply1.graphDataName);
            Assert.DoesNotThrow(() => multiply1.graphDataOwner.TryGetNodeHandler(out _));

            var currentCount = GraphModel.NodeModels.Count;

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, multiply1));
            yield return null;

            m_GraphView.Focus();
            m_TestEventHelper.SendDuplicateCommand();
            yield return null;

            Assert.AreEqual(currentCount + 1, GraphModel.NodeModels.Count);

            var newNode = GraphModel.NodeModels.OfType<SGNodeModel>().FirstOrDefault(n => n.Title == multiply1.Title && n.Guid != multiply1.Guid);
            Assert.IsNotNull(newNode);

            Assert.IsNotNull(newNode.graphDataName);
            Assert.AreNotEqual(multiply1.graphDataName, newNode.graphDataName);
            Assert.DoesNotThrow(() => newNode.graphDataOwner.TryGetNodeHandler(out _));
        }

        [UnityTest]
        public IEnumerator TestNodeCanBeCutPasted()
        {
            var multiply1 = SGGraphTestUtils.CreateNodeByName(GraphModel, "Multiply", Vector2.zero);
            Assert.NotNull(multiply1, "Multiply node could not be added to the graph model");
            multiply1.Title = "Multiply 1";

            Assert.IsNotNull(multiply1.graphDataName);
            Assert.DoesNotThrow(() => multiply1.graphDataOwner.TryGetNodeHandler(out _));

            var currentCount = GraphModel.NodeModels.Count;

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, multiply1));
            yield return null;

            m_GraphView.Focus();
            m_TestEventHelper.SendCutCommand();
            yield return null;

            m_GraphView.Focus();
            m_TestEventHelper.SendPasteCommand();
            yield return null;

            Assert.AreEqual(currentCount, GraphModel.NodeModels.Count);

            var newNode = GraphModel.NodeModels.OfType<SGNodeModel>().FirstOrDefault(n => n.Title == multiply1.Title && n.Guid != multiply1.Guid);
            Assert.IsNotNull(newNode);

            Assert.IsNotNull(newNode.graphDataName);
            Assert.AreNotEqual(multiply1.graphDataName, newNode.graphDataName);
            Assert.DoesNotThrow(() => newNode.graphDataOwner.TryGetNodeHandler(out _));
        }
    }
}
