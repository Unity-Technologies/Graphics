using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;
using UnityEngine;
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
            return AddNodeFromSearcherAndValidate("Add");
        }

        [UnityTest]
        public IEnumerator NodeCollapseExpandTest()
        {
            yield return AddNodeFromSearcherAndValidate("Add");

            var nodeModel = GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModel)
            {
                var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
                Assert.IsNotNull(nodeGraphElement);

                // Test the collapse button
                var collapseButton = nodeGraphElement.Q("collapse");
                Assert.IsNotNull(collapseButton);

                var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, collapseButton, true);
                m_ShaderGraphWindowTestHelper.SimulateMouseClick(collapseButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);

                // Test the expand button
                var expandButton = nodeGraphElement.Q("expand");
                Assert.IsNotNull(expandButton);

                var expandButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, expandButton, true);
                m_ShaderGraphWindowTestHelper.SimulateMouseClick(expandButtonPosition);
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
            yield return AddNodeFromSearcherAndValidate("Add");

            var nodeModel = GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            if (nodeModel is GraphDataNodeModel graphDataNodeModel)
            {
                var nodeGraphElement = m_GraphView.GetGraphElement(graphDataNodeModel);
                Assert.IsNotNull(nodeGraphElement);

                // Test the collapse button
                var collapseButton = nodeGraphElement.Q("collapse");
                Assert.IsNotNull(collapseButton);

                var collapseButtonPosition = TestEventHelpers.GetScreenPosition(m_Window, collapseButton, true);
                m_ShaderGraphWindowTestHelper.SimulateMouseClick(collapseButtonPosition);
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                Assert.IsFalse(graphDataNodeModel.IsPreviewExpanded);
            }

            yield return SaveAndReopenGraph();

            nodeModel = GetNodeModelFromGraphByName("Add");
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

            Assert.IsTrue(m_ShaderGraphWindowTestHelper.SendDeleteCommand());
            yield return null;

            var afterContext = GetNodeModelFromGraphByName(beforeContext.Title);
            Assert.AreEqual(beforeContext, afterContext, "Context node should be unaffected by delete operation");
        }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeDeletedFromMixedSelection()
        {
            var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            var beforeContextCount = beforeContexts.Count;
            Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");

            // Arbitrary node so that something other than a context exists in our graph.
            yield return AddNodeFromSearcherAndValidate("Add");

            Assert.IsTrue(m_ShaderGraphWindowTestHelper.SendDeleteCommand());
            Assert.IsFalse(FindNodeOnGraphByName("Add"), "Non-context node should be deleted from selection");

            var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            Assert.AreEqual(beforeContexts.Count, afterContexts.Count, "Context nodes should not be deleted from selection");
        }

        [UnityTest]
        public IEnumerator TestContextNodesCannotBeCopied()
        {
            var beforeContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            var beforeContextCount = beforeContexts.Count;
            Assert.IsTrue(beforeContextCount > 0, "Graph must contain at least one context node for test");

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, beforeContexts[0]));
            yield return null;

            m_ShaderGraphWindowTestHelper.SimulateKeyPress("C", modifiers: EventModifiers.Control);
            yield return null;

            m_ShaderGraphWindowTestHelper.SimulateKeyPress("V", modifiers: EventModifiers.Control);
            yield return null;

            var afterContexts = m_GraphView.GraphModel.NodeModels.OfType<GraphDataContextNodeModel>().ToList();
            Assert.AreEqual(beforeContexts.Count, afterContexts.Count, "Context node should not be duplicated by copy/paste");
        }

        [UnityTest]
        public IEnumerator TestOutdatedNodeGetsUpgradeBadge()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 1},
                displayName: "V1"
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
                new RegistryKey {Name = "TestUpgrade", Version = 2},
                displayName: "V2"
            );
            yield return null;

            var errors = m_GraphView.GraphTool.GraphProcessingState.Errors;
            Assert.IsTrue(errors.Count == 0, "Up-to-date node should not have any warnings");
        }

        [UnityTest]
        public IEnumerator TestNodeCanBeUpgraded()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 1},
                displayName: "V1"
            );
            yield return null;

            m_GraphView.Dispatch(new UpgradeNodeCommand(node));
            yield return null;

            Assert.AreEqual(2, node.registryKey.Version, "Upgrading a node should set it to the latest version");
        }

        [UnityTest]
        public IEnumerator TestDismissingUpgradeRemovesBadge()
        {
            var node = m_GraphView.GraphModel.CreateGraphDataNode(
                new RegistryKey {Name = "TestUpgrade", Version = 1},
                displayName: "V1"
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
            yield return AddNodeFromSearcherAndValidate("Add");

            var nodeModel = GetNodeModelFromGraphByName("Add");
            Assert.IsNotNull(nodeModel);

            // Select element programmatically because it might be behind another one
            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel));
            yield return null;

            Assert.IsTrue(m_ShaderGraphWindowTestHelper.SendDeleteCommand());
            yield return null;

            var addNode = GetNodeModelFromGraphByName("Add");
            Assert.IsNull(addNode, "Node should be null after delete operation");
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
    }
}
