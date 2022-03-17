using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class NonInteractiveGraphViewTests : GraphViewTester
    {
        protected override void CreateWindow()
        {
            Window = EditorWindow.GetWindowWithRect<NonInteractiveTestGraphViewWindow>(k_WindowRect);
            Window.CloseAllOverlays();
        }

        [UnityTest]
        public IEnumerator CannotMoveNodeInNonInteractiveMode()
        {
            var node = CreateNode("Node", new Vector2(100, 100));
            MarkGraphViewStateDirty();
            yield return null;

            var nodeModelPosition = node.Position;

            var nodeUI = node.GetView(GraphView);
            Assert.IsNotNull(nodeUI);

            var nodeUIPosition = nodeUI.layout.position;

            // Move!
            {
                var worldPosition = GraphView.ContentViewContainer.LocalToWorld(nodeUIPosition);
                var start = worldPosition + new Vector2(35, 35);
                var end = start + new Vector2(50, 50);
                Helpers.DragTo(start, end);
                yield return null;
            }

            yield return null;

            Assert.AreEqual(nodeModelPosition, node.Position);
            Assert.AreEqual(nodeUIPosition, nodeUI.layout.position);
        }

        [UnityTest]
        public IEnumerator ElementIsNotHoveredInNonInteractiveMode()
        {
            var node = CreateNode("Node", new Vector2(100, 100));
            Helpers.MouseMoveEvent(Vector2.zero, new Vector2(-100, -100));
            MarkGraphViewStateDirty();
            yield return null;

            var nodeUI = node.GetView(GraphView);
            Assert.IsNotNull(nodeUI);
            var children = nodeUI.hierarchy.Children();
            var selectionBorder = children.OfType<SelectionBorder>().FirstOrDefault();
            Assert.IsNotNull(selectionBorder);
            var initialBorderColor = selectionBorder.resolvedStyle.borderBottomColor;

            var nodeUIPosition = nodeUI.layout.position;
            var worldPosition = nodeUI.parent.LocalToWorld(nodeUIPosition);
            Helpers.MouseMoveEvent(new Vector2(-100, -100), worldPosition + new Vector2(50, 50));
            yield return null;

            Assert.AreEqual(initialBorderColor, selectionBorder.resolvedStyle.borderBottomColor);
        }
    }
}
