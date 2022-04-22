using System;
using System.Collections;
using NUnit.Framework;
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
