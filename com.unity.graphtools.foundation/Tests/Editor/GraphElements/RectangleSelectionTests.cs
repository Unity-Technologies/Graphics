using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class RectangleSelectionTests : GraphViewTester
    {
        TokenNode token1;
        TokenNode token2;
        Node node;
        Edge edge1;
        Edge edge2;

        IEnumerator InitRectangleSelectionTest()
        {
            var decl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "f0", ModifierFlags.Read, true);
            var var1 = GraphModel.CreateVariableNode(decl, Vector2.zero);
            var var2 = GraphModel.CreateVariableNode(decl, Vector2.up * 350);
            var nodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("input", new Vector2(400, 100));
            var edgeModel1 = GraphModel.CreateEdge(nodeModel.Input0, var1.OutputPort);
            var edgeModel2 = GraphModel.CreateEdge(nodeModel.Input1, var2.OutputPort);
            MarkGraphViewStateDirty();
            yield return null;
            token1 = var1.GetView<TokenNode>(GraphView);
            token2 = var2.GetView<TokenNode>(GraphView);
            node = nodeModel.GetView<Node>(GraphView);
            edge1 = edgeModel1.GetView<Edge>(GraphView);
            edge2 = edgeModel2.GetView<Edge>(GraphView);
        }

        [UnityTest]
        public IEnumerator RectangleSelectionSelectsNodesAndEdges()
        {
            yield return InitRectangleSelectionTest();

            Rect rectangle = RectTestUtils.RectAroundElements(token1, token2, node);

            Helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.True(token1.IsSelected());
            Assert.True(token2.IsSelected());
            Assert.True(node.IsSelected());
            Assert.True(edge1.IsSelected());
            Assert.True(edge2.IsSelected());
        }

        [UnityTest]
        public IEnumerator RectangleSelectionOneNodeDoesntSelectEdges()
        {
            yield return InitRectangleSelectionTest();

            Rect rectangle = RectTestUtils.RectAroundElements(node);

            Helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.False(token1.IsSelected());
            Assert.False(token2.IsSelected());
            Assert.True(node.IsSelected());
            Assert.False(edge1.IsSelected());
            Assert.False(edge2.IsSelected());
        }

        [UnityTest]
        public IEnumerator RectangleSelectionTwoNodesSelectOnlyRelevantEdges()
        {
            yield return InitRectangleSelectionTest();

            // drawing selection around node and token1 will cross both edges
            Rect rectangle = RectTestUtils.RectAroundElements(node, token1);

            Helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.True(token1.IsSelected());
            Assert.False(token2.IsSelected());
            Assert.True(node.IsSelected());
            Assert.True(edge1.IsSelected());
            Assert.False(edge2.IsSelected());
        }

        [UnityTest]
        public IEnumerator RectangleSelectionEdgesOnlyWorks()
        {
            yield return InitRectangleSelectionTest();

            // draw selection on edges only
            Rect rectangle = new Rect(token1.localBound.xMax + 1,
                token1.localBound.yMin,
                node.localBound.xMin - token1.localBound.xMax - 2,
                token2.localBound.yMax - token1.localBound.yMin);

            Helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.False(token1.IsSelected());
            Assert.False(token2.IsSelected());
            Assert.False(node.IsSelected());
            Assert.True(edge1.IsSelected());
            Assert.True(edge2.IsSelected());
        }
    }
}
