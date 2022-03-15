using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class ContinuouslyUpdatingWindow : UITestWindow
    {
        protected override void Update()
        {
            var graph = GraphTool?.ToolState.GraphModel;
            if (graph != null)
            {
                var nodes = graph.NodeModels.OfType<NodeModel>();
                foreach (var node in nodes)
                {
                    using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
                    {
                        var c = node.Color;
                        c.a = 1f;
                        c.r += 0.01f;
                        c.g += 0.0104f;
                        c.b += 0.0101f;
                        if (c.r >= 1f)
                            c.r = 0f;
                        if (c.g >= 1f)
                            c.g = 0f;
                        if (c.b >= 1f)
                            c.b = 0f;
                        node.Color = c;

                        updater.MarkChanged(node, ChangeHint.Style);
                    }
                }
            }

            base.Update();
        }
    }

    class ContinuousUpdateTests : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        protected override Type GetWindowType() => typeof(ContinuouslyUpdatingWindow);

        [UnityTest]
        public IEnumerator GraphViewPansEvenIfModelChangesContinuously()
        {
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(50, 200));
            var originalColor = node.Color;
            var originalGraphViewPan = GraphView.GraphViewModel.GraphViewState.Position;
            Assert.AreEqual(Vector3.zero, originalGraphViewPan);

            MarkGraphModelStateDirty();
            yield return null;

            var nodeUI = node.GetView(GraphView);
            Assert.IsNotNull(nodeUI);

            var currentPosition = nodeUI.layout.center;
            Helpers.MouseDownEvent(nodeUI.layout.center);
            yield return null;

            // Move the node on the window edge.
            Helpers.MouseMoveEvent(currentPosition, currentPosition + Vector2.left * currentPosition.x);

            // Let the graph view pan for a few frames.
            for (var i = 0; i < 100; i++)
            {
                yield return null;
            }

            Helpers.MouseUpEvent(currentPosition);
            yield return null;

            // Offset varies but should be large enough.
            Assert.GreaterOrEqual(GraphView.GraphViewModel.GraphViewState.Position.x, 190);
            //Check that the node was actually continuously updating.
            Assert.AreNotEqual(originalColor, node.Color);
        }

        [UnityTest]
        public IEnumerator NodeIsMovedEvenIfModelChangesContinuously()
        {
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);
            var originalColor = node.Color;
            MarkGraphModelStateDirty();
            yield return null;

            var nodeUI = node.GetView(GraphView);
            Assert.IsNotNull(nodeUI);

            var currentPosition = nodeUI.layout.center;
            Helpers.MouseDownEvent(nodeUI.layout.center);
            yield return null;

            var nextPosition = currentPosition + Vector2.right;
            Helpers.MouseMoveEvent(currentPosition, nextPosition);
            yield return null;

            currentPosition = nextPosition;
            nextPosition = currentPosition + Vector2.right;
            Helpers.MouseMoveEvent(currentPosition, nextPosition);
            yield return null;

            currentPosition = nextPosition;
            nextPosition = currentPosition + Vector2.right;
            Helpers.MouseMoveEvent(currentPosition, nextPosition);
            yield return null;

            Helpers.MouseUpEvent(nextPosition);
            yield return null;

            Assert.AreEqual(Vector2.one * 200 + Vector2.right * 3, node.Position);
            //Check that the node was actually continuously updating.
            Assert.AreNotEqual(originalColor, node.Color);
        }
    }
}
