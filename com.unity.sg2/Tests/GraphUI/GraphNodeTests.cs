using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    public class GraphNodeTests : BaseGraphWindowTest
    {
        [UnityTest]
        public IEnumerator CreateAddNodeFromSearcherTest()
        {
            return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        }

        [UnityTest]
        public IEnumerator NodeCollapseExpandTest()
        {
            yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModel)
            {
                var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
                Assert.IsNotNull(nodeGraphElement);

                // Test the collapse button
                var collapseButton = nodeGraphElement.Q("collapse");
                Assert.IsNotNull(collapseButton);

                var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, collapseButton, true);
                m_TestEventHelper.SimulateMouseClick(collapseButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);

                // Test the expand button
                var expandButton = nodeGraphElement.Q("expand");
                Assert.IsNotNull(expandButton);

                var expandButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, expandButton, true);
                m_TestEventHelper.SimulateMouseClick(expandButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsTrue(graphDataNodeModel.IsPreviewExpanded);
            }
        }

        [UnityTest]
        public IEnumerator NodeCollapseStateSerializationTest()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModel)
            {
                var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
                Assert.IsNotNull(nodeGraphElement);

                // Test the collapse button
                var collapseButton = nodeGraphElement.Q("collapse");
                Assert.IsNotNull(collapseButton);

                var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, collapseButton, true);
                m_TestEventHelper.SimulateMouseClick(collapseButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);
            }

            yield return SaveAndReopenGraph();

            nodeModel = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModelReloaded)
                Assert.IsFalse(graphDataNodeModelReloaded.IsPreviewExpanded);
        }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeDeleted()
        {
            var beforeContext = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().FirstOrDefault();
            Assert.IsNotNull(beforeContext, "Graph must contain at least one context node for test");

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, beforeContext));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            var afterContext = m_Window.GetNodeModelFromGraphByName(beforeContext.Title);
            Assert.AreEqual(beforeContext, afterContext, "Context node should be unaffected by delete operation");
        }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeDeletedFromMixedSelection()
        {
            var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            var beforeContextCount = beforeContexts.Count;
            Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");

            // Arbitrary node so that something other than a context exists in our graph.
            yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
            var nodeModel = m_Window.GetNodeModelFromGraphByName("Add");

            // Select the context nodes and the add node
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, beforeContexts.Append(nodeModel).ToList()));

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            Assert.IsNull(m_Window.GetNodeModelFromGraphByName("Add"), "Non-context node should be deleted from selection");

            var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            Assert.AreEqual(beforeContexts.Count, afterContexts.Count, "Context nodes should not be deleted from selection");
        }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeCopied()
        {
            var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            var beforeContextCount = beforeContexts.Count;
            Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");

            yield return m_TestInteractionHelper.SelectAndCopyNodes(new List<INodeModel>() { beforeContexts[0] });

            var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
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

        [UnityTest]
        public IEnumerator TestNodeCanBeDeleted()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            var addNode = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNull(addNode, "Node should be null after delete operation");

            var graphDataNodeModel = nodeModel as GraphDataNodeModel;
            var addNodeHandler = GraphModel.GraphHandler.GetNode(graphDataNodeModel.graphDataName);
            Assert.IsNull(addNodeHandler, "Node should also be removed from CLDS after delete operation");
        }

        [UnityTest]
        public IEnumerator TestNodeCanBeCopied()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModel = m_Window.GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            yield return m_TestInteractionHelper.SelectAndCopyNodes(new List<INodeModel>() { nodeModel });

            Assert.IsTrue(m_Window.GetNodeModelsFromGraphByName("Add").Count == 2);
        }

        [UnityTest]
        public IEnumerator TestMultipleNodesCanBeCopied()
        {
            // Create two Add nodes
            yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
            yield return  m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            var nodeModels = m_Window.GetNodeModelsFromGraphByName("Add");

            yield return m_TestInteractionHelper.SelectAndCopyNodes(nodeModels);

            Assert.IsTrue(m_Window.GetNodeModelsFromGraphByName("Add").Count == 4);
        }

        /*
        /* This test needs the ability to distinguish between nodes and non-node graph elements like the Sticky Note
        /* When we have categories for the searcher items we can distinguish between them
        [UnityTest]
        public IEnumerator CreateAllNodesFromSearcherTest()
        {
            if (m_Window.GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
            {
                var shaderGraphStencil = shaderGraphModel.Stencil as ShaderGraphStencil;
                var searcherDatabaseProvider = new ShaderGraphSearcherDatabaseProvider(shaderGraphStencil);
                var searcherDatabases = searcherDatabaseProvider.GetGraphElementsSearcherDatabases(shaderGraphModel);
                foreach (var database in searcherDatabases)
                {
                    foreach (var searcherItem in database.Search(""))
                    {
                        return AddNodeFromSearcherAndValidate(searcherItem.Name);
                    }
                }
            }

            return null;
        }
        */

        [UnityTest]
        public IEnumerator TestDynamicPortsUpdate()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply = (GraphDataNodeModel)m_Window.GetNodeModelFromGraphByName("Multiply");

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector 2");
            var vec2 = (GraphDataNodeModel)m_Window.GetNodeModelFromGraphByName("Vector 2");

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            m_GraphView.Dispatch(new CreateEdgeCommand(multiply.InputsById["A"], vec2.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Multiply node connected to Vector 2 should show Vector 2 type");
            }

            var createdEdge = vec2.GetConnectedEdges().First();
            m_GraphView.Dispatch(new DeleteEdgeCommand(createdEdge));
            yield return null;

            foreach (var port in multiply.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "After disconnecting edges, multiply node should default to Float");
            }
        }

        [UnityTest]
        public IEnumerator TestDynamicPortUpdatesPropagate()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply1 = (GraphDataNodeModel)m_Window.GetNodeModelFromGraphByName("Multiply");
            multiply1.Title = "Multiply 1";

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Multiply");
            var multiply2 = (GraphDataNodeModel)m_Window.GetNodeModelFromGraphByName("Multiply");
            multiply2.Title = "Multiply 2";

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector 2");
            var vec2 = (GraphDataNodeModel)m_Window.GetNodeModelFromGraphByName("Vector 2");

            m_GraphView.Dispatch(new CreateEdgeCommand(multiply2.InputsById["A"], multiply1.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply1.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            foreach (var port in multiply2.Ports)
            {
                Assert.AreEqual(TypeHandle.Float, port.DataTypeHandle, "Multiply node should default to Float");
            }

            m_GraphView.Dispatch(new CreateEdgeCommand(multiply1.InputsById["A"], vec2.OutputsById["Out"]));
            yield return null;

            foreach (var port in multiply1.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Multiply node connected to Vector 2 should show Vector 2 type");
            }

            foreach (var port in multiply2.Ports)
            {
                Assert.AreEqual(TypeHandle.Vector2, port.DataTypeHandle, "Second multiply node in a series should react to upstream change to Vector 2");
            }

            var createdEdge = vec2.GetConnectedEdges().First();
            m_GraphView.Dispatch(new DeleteEdgeCommand(createdEdge));
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
