using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementAttachTests : GraphViewTester
    {
        static readonly Rect k_NodeRect = new Rect(Overdrive.GraphView.panAreaWidth * 2, Overdrive.GraphView.panAreaWidth * 3, 50, 50);

        Attacher CreateAttachedElement(VisualElement target)
        {
            Attacher attacher = null;
            if (target != null)
            {
                VisualElement attached = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute,
                        width = 10,
                        height = 10,
                        backgroundColor = Color.blue
                    }
                };

                target.parent.Add(attached);
                attacher = new Attacher(attached, target, SpriteAlignment.LeftCenter);
            }

            return attacher;
        }

        [UnityTest]
        public IEnumerator AttachedElementIsPlacedProperlyAndFollowsNode()
        {
            // Create node.
            var nodeModel = CreateNode("Node 1", k_NodeRect.position);
            MarkGraphViewStateDirty();
            GraphView.Dispatch(new ReframeGraphViewCommand(Vector3.zero, Vector3.one));
            yield return null;

            var node = nodeModel.GetView<Node>(GraphView);
            node.style.width = k_NodeRect.width;
            node.style.height = k_NodeRect.height;

            var attacher = CreateAttachedElement(node);
            Assert.AreNotEqual(null, attacher);
            yield return null;

            var initialPosition = nodeModel.Position;
            Assert.AreEqual(attacher.Target.layout.center.y, attacher.Element.layout.center.y);
            Assert.AreNotEqual(attacher.Target.layout.center.x, attacher.Element.layout.center.x);

            var mouseDownPosition = initialPosition + k_NodeRect.size / 2;
            mouseDownPosition = GraphView.contentContainer.LocalToWorld(mouseDownPosition);
            var delta = Vector2.one * 10;
            // Move the movable node.
            Helpers.MouseDownEvent(mouseDownPosition);
            yield return null;

            Helpers.MouseDragEvent(mouseDownPosition, mouseDownPosition + delta);
            yield return null;

            Helpers.MouseUpEvent(mouseDownPosition + delta);
            yield return null;

            Assert.AreNotEqual(initialPosition, nodeModel.Position);
            Assert.AreEqual(attacher.Target.layout.center.y, attacher.Element.layout.center.y);
            Assert.AreNotEqual(attacher.Target.layout.center.x, attacher.Element.layout.center.x);

            yield return null;
        }
    }
}
