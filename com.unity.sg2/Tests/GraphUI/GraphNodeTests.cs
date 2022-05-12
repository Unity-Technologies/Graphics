using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
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

            // Save graph and close the window
            GraphAssetUtils.SaveOpenGraphAsset(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            // Reload the graph asset
            var graphAsset = ShaderGraphAsset.HandleLoad(testAssetPath);
            CreateWindow();
            m_Window.Show();
            m_Window.Focus();
            m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            yield return null;

            // Wait till the graph model is loaded back up
            while (m_Window.GraphView.GraphModel == null)
                yield return null;

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

            m_ShaderGraphWindowTestHelper.SimulateKeyPress(KeyCode.Delete);
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

            m_GraphView.Dispatch(new DeleteElementsCommand(m_GraphView.GraphModel.NodeModels));
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
