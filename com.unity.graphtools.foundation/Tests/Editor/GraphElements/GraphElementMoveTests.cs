using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementMoveTests : GraphViewTester
    {
        static readonly Vector2 k_NodePos = new Vector2(SelectionDragger.panAreaWidth * 2, SelectionDragger.panAreaWidth * 3);
        static readonly Rect k_MinimapRect = new Rect(100, 100, 100, 100);
        Vector2 m_SelectionOffset = new Vector2(100, 100);

        INodeModel NodeModel { get; set; }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;

            NodeModel = CreateNode("Movable element", k_NodePos, 0, 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.style.left = k_MinimapRect.x;
            miniMap.style.top = k_MinimapRect.y;
            miniMap.style.width = k_MinimapRect.width;
            miniMap.style.height = k_MinimapRect.height;
            GraphView.Add(miniMap);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            GraphViewStaticBridge.SetTimeSinceStartupCallback(null);
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDragged()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var node = NodeModel.GetView<Node>(GraphView);
            Assert.IsNotNull(node);

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;
            Vector2 moveOffset = new Vector2(10, 10);

            // Move the movable element.
            Helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            Assert.AreEqual(k_NodePos.x + moveOffset.x, node.layout.x);
            Assert.AreEqual(k_NodePos.y + moveOffset.y, node.layout.y);
        }

        [UnityTest]
        public IEnumerator LocallyScaledElementMovesAtSameSpeed()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            float scaling = 2.0f;
            node.transform.scale = new Vector3(scaling, scaling, scaling);

            yield return MovableElementCanBeDragged();
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAndMoveCancelledByEscapeKey()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            bool needsMouseUp = false;
            bool needsKeyUp = false;

            try
            {
                GraphElement elem = GraphView.SafeQ<GraphElement>();

                Vector2 startElementPosition = node.layout.position;

                Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);

                Vector2 start = worldNodePos + m_SelectionOffset;
                Vector2 move = new Vector2(-10, -10);
                // Move the movable element.
                Helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                Helpers.MouseDragEvent(start, start + move);
                yield return null;

                Vector2 endElementPosition = elem.layout.position;

                Assert.AreEqual(move, (endElementPosition - startElementPosition));

                Helpers.KeyDownEvent(KeyCode.Escape);
                needsKeyUp = true;
                yield return null;

                // Back where we started
                //we test both visual and presenter values
                Assert.AreEqual(node.layout.position, startElementPosition);
                Assert.AreEqual(elem.layout.position, startElementPosition);

                Helpers.KeyUpEvent(KeyCode.Escape);
                needsKeyUp = false;
                yield return null;

                Helpers.MouseUpEvent(start + move);
                needsMouseUp = false;
                yield return null;

                endElementPosition = node.layout.position;
                // We make sure nothing was committed
                Assert.AreEqual(endElementPosition, startElementPosition);
            }
            finally
            {
                if (needsKeyUp)
                    Helpers.KeyUpEvent(KeyCode.Escape);
                if (needsMouseUp)
                    Helpers.MouseUpEvent(new Vector2(k_NodePos.x - 5, k_NodePos.y + 15));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInNegativeX()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;
            Vector2 panPos = new Vector2(SelectionDragger.panAreaWidth, start.y);

            try
            {
                // Move the movable element.
                Helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = GraphView.SafeQ<GraphElement>();

                float deltaX = start.x - panPos.x;
                var newPos = new Vector2(k_NodePos.x - deltaX, k_NodePos.y);

                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.panSpeed * SelectionDragger.minSpeedFactor;

                yield return null;
                newPos.x -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(panSpeedAtLocation, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                yield return null;
                newPos.x -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(panSpeedAtLocation * 2, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.layout.position);

                Assert.AreEqual(panSpeedAtLocation * 2, GraphView.ContentViewContainer.transform.position.x);
                Assert.AreEqual(0, GraphView.ContentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInPositiveX()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;
            Vector2 panPos = new Vector2(Window.position.width - SelectionDragger.panAreaWidth, start.y);

            try
            {
                // Move the movable element.
                Helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = GraphView.SafeQ<GraphElement>();

                float deltaX = start.x - panPos.x;

                var newPos = new Vector2(k_NodePos.x - deltaX, k_NodePos.y);

                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.panSpeed * SelectionDragger.minSpeedFactor;

                yield return null;
                newPos.x += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(-panSpeedAtLocation, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                yield return null;
                newPos.x += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(-panSpeedAtLocation * 2, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.layout.position);

                Assert.AreEqual(-panSpeedAtLocation * 2, GraphView.ContentViewContainer.transform.position.x);
                Assert.AreEqual(0, GraphView.ContentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInNegativeY()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            Vector2 worldPanPos =
                GraphView.ContentViewContainer.LocalToWorld(
                    new Vector2(SelectionDragger.panAreaWidth, SelectionDragger.panAreaWidth));
            Vector2 panPos = new Vector2(start.x, worldPanPos.y);

            try
            {
                // Move the movable element.
                Helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = GraphView.SafeQ<GraphElement>();

                float deltaY = start.y - panPos.y;
                Vector2 newPos = new Vector2(k_NodePos.x, k_NodePos.y - deltaY);

                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.panSpeed * SelectionDragger.minSpeedFactor;

                yield return null;
                newPos.y -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(panSpeedAtLocation, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                yield return null;
                newPos.y -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(panSpeedAtLocation * 2, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.layout.position);

                Assert.AreEqual(0, GraphView.ContentViewContainer.transform.position.x);
                Assert.AreEqual(panSpeedAtLocation * 2, GraphView.ContentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInPositiveY()
        {
            MarkGraphViewStateDirty();
            yield return null;
            var node = NodeModel.GetView<Node>(GraphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;
            float worldY = GraphView.LocalToWorld(new Vector2(0, GraphView.layout.height - SelectionDragger.panAreaWidth)).y;
            Vector2 panPos = new Vector2(start.x, worldY);

            try
            {
                // Move the movable element.
                Helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(start, panPos);
                yield return null;

                float deltaY = start.y - panPos.y;
                Vector2 newPos = new Vector2(k_NodePos.x, k_NodePos.y - deltaY);

                GraphElement elem = GraphView.SafeQ<GraphElement>();

                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.panSpeed * SelectionDragger.minSpeedFactor;

                yield return null;
                newPos.y += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(-panSpeedAtLocation, GraphView.ViewTransform.position.y);

                timePassed += SelectionDragger.panInterval;
                GraphView.UpdateScheduledEvents();

                yield return null;
                newPos.y += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.layout.position);

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(-panSpeedAtLocation * 2, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.layout.position);

                Assert.AreEqual(0, GraphView.ContentViewContainer.transform.position.x);
                Assert.AreEqual(-panSpeedAtLocation * 2, GraphView.ContentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInNegativeX()
        {
            MarkGraphViewStateDirty();
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = Window.GraphView.SafeQ<Port>();

                portCenter = port.GetGlobalCenter();

                Helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(portCenter, new Vector2(EdgeDragHelper.panAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = EdgeDragHelper.panSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                // Move outside window
                Helpers.MouseDragEvent(new Vector2(EdgeDragHelper.panAreaWidth, portCenter.y),
                    new Vector2(-EdgeDragHelper.panAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                panSpeedAtLocation = EdgeDragHelper.maxPanSpeed;
                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(new Vector2(-EdgeDragHelper.panAreaWidth, portCenter.y));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(new Vector2(-EdgeDragHelper.panAreaWidth, portCenter.y));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInPositiveX()
        {
            MarkGraphViewStateDirty();
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = Window.GraphView.SafeQ<Port>();

                portCenter = port.GetGlobalCenter();

                Helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Helpers.MouseDragEvent(portCenter,
                    new Vector2(Window.position.width - (EdgeDragHelper.panAreaWidth), portCenter.y));
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = -EdgeDragHelper.panSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                // Move outside window
                Helpers.MouseDragEvent(
                    new Vector2(Window.position.width - EdgeDragHelper.panAreaWidth, portCenter.y),
                    new Vector2(Window.position.width + EdgeDragHelper.panAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                panSpeedAtLocation = -EdgeDragHelper.maxPanSpeed;
                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(new Vector2(Window.position.width + EdgeDragHelper.panAreaWidth, portCenter.y));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(new Vector2(Window.position.width + EdgeDragHelper.panAreaWidth, portCenter.y));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInNegativeY()
        {
            MarkGraphViewStateDirty();
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = Window.GraphView.SafeQ<Port>();

                portCenter = port.GetGlobalCenter();

                Helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                Vector2 originalWorldNodePos = GraphView.ContentViewContainer.LocalToWorld(new Vector2(portCenter.x, EdgeDragHelper.panAreaWidth));
                Helpers.MouseDragEvent(portCenter, originalWorldNodePos);
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = EdgeDragHelper.panSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                // Move outside window
                Vector2 newWorldNodePos = new Vector2(portCenter.x, -EdgeDragHelper.panAreaWidth);
                Helpers.MouseDragEvent(originalWorldNodePos, newWorldNodePos);
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                panSpeedAtLocation = EdgeDragHelper.maxPanSpeed;
                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(newWorldNodePos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(new Vector2(portCenter.x, -EdgeDragHelper.panAreaWidth));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInPositiveY()
        {
            MarkGraphViewStateDirty();
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;
            var worldY = 0.0f;

            try
            {
                var port = Window.GraphView.SafeQ<Port>();

                portCenter = port.GetGlobalCenter();

                Helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                // ReSharper disable once AccessToModifiedClosure
                GraphViewStaticBridge.SetTimeSinceStartupCallback(() => timePassed);

                worldY = GraphView.LocalToWorld(new Vector2(0, GraphView.layout.height - EdgeDragHelper.panAreaWidth)).y;
                Helpers.MouseDragEvent(portCenter,
                    new Vector2(portCenter.x, worldY));
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(0, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                float panSpeedAtLocation = -EdgeDragHelper.panSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                // Move outside window
                Helpers.MouseDragEvent(
                    new Vector2(portCenter.x, worldY),
                    new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.panAreaWidth));
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                panSpeedAtLocation = -EdgeDragHelper.maxPanSpeed;
                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                timePassed += EdgeDragHelper.panInterval;
                GraphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);

                Helpers.MouseUpEvent(new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.panAreaWidth));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(0, GraphView.ViewTransform.position.x);
                Assert.AreEqual(totalDisplacement, GraphView.ViewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.panAreaWidth));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PanSpeedsAreAsExpected()
        {
            MarkGraphViewStateDirty();
            yield return null;

            float minSpeed = SelectionDragger.minSpeedFactor * SelectionDragger.panSpeed;
            float midwaySpeed = (SelectionDragger.maxSpeedFactor + SelectionDragger.minSpeedFactor) / 2 * SelectionDragger.panSpeed;
            float maxSpeed = SelectionDragger.maxSpeedFactor * SelectionDragger.panSpeed;

            float maxDiagonalDistance = (float)Math.Sqrt((SelectionDragger.maxSpeedFactor * SelectionDragger.panSpeed) * (SelectionDragger.maxSpeedFactor * SelectionDragger.panSpeed) / 2);

            // safeDistance is a distance at which the component it is applied to (x or y) will have no effect on auto panning.
            float safeDistance = SelectionDragger.panAreaWidth * 2;

            float worldY = GraphView.ChangeCoordinatesTo(GraphView.contentContainer, new Vector2(0, GraphView.layout.height)).y;

            Vector2[][] testData =
            {
                // LEFT
                new[] // Test 0: Inside, in X to the left
                {
                    new Vector2(SelectionDragger.panAreaWidth, safeDistance),
                    new Vector2(-minSpeed, 0)
                },
                new[] // Test 1: At the border, in X to the left
                {
                    new Vector2(0, safeDistance),
                    new Vector2(-midwaySpeed, 0)
                },
                new[] // Test 2: Outside, in X to the left
                {
                    new Vector2(-SelectionDragger.panAreaWidth, safeDistance),
                    new Vector2(-maxSpeed, 0)
                },

                // RIGHT
                new[] // Test 3: Inside, in X to the right
                {
                    new Vector2(Window.position.width - SelectionDragger.panAreaWidth, safeDistance),
                    new Vector2(minSpeed, 0)
                },
                new[] // Test 4: At the border, in X to the right
                {
                    new Vector2(Window.position.width, safeDistance),
                    new Vector2(midwaySpeed, 0)
                },
                new[] // Test 5: Outside, in X to the right
                {
                    new Vector2(Window.position.width + SelectionDragger.panAreaWidth, safeDistance),
                    new Vector2(maxSpeed, 0)
                },

                // TOP
                new[] // Test 6: Inside, in Y to the top
                {
                    new Vector2(safeDistance, SelectionDragger.panAreaWidth),
                    new Vector2(0, -minSpeed)
                },
                new[] // Test 7: At the border, in Y to the top
                {
                    new Vector2(safeDistance, 0),
                    new Vector2(0, -midwaySpeed)
                },
                new[] // Test 8: Outside, in Y to the top
                {
                    new Vector2(safeDistance, -SelectionDragger.panAreaWidth),
                    new Vector2(0, -maxSpeed)
                },

                // BOTTOM
                new[] // Test 9: Inside, in Y to the bottom
                {
                    new Vector2(safeDistance, worldY - SelectionDragger.panAreaWidth),
                    new Vector2(0, minSpeed)
                },
                new[] // Test 10: At the border, in X to the bottom
                {
                    new Vector2(safeDistance, worldY),
                    new Vector2(0, midwaySpeed)
                },
                new[] // Test 11: Outside, in X to the bottom
                {
                    new Vector2(safeDistance, worldY + SelectionDragger.panAreaWidth),
                    new Vector2(0, maxSpeed)
                },

                // CORNERS
                new[] // Test 12: Extreme top left corner
                {
                    new Vector2(-SelectionDragger.panAreaWidth * 5, -SelectionDragger.panAreaWidth * 5),
                    new Vector2(-maxDiagonalDistance, -maxDiagonalDistance)
                },
                new[] // Test 12: Extreme top right corner
                {
                    new Vector2(-SelectionDragger.panAreaWidth * 5, worldY + SelectionDragger.panAreaWidth * 5),
                    new Vector2(-maxDiagonalDistance, maxDiagonalDistance)
                },
                new[] // Test 12: Extreme bottom right corner
                {
                    new Vector2(Window.position.width + SelectionDragger.panAreaWidth * 5, worldY + SelectionDragger.panAreaWidth * 5),
                    new Vector2(maxDiagonalDistance, maxDiagonalDistance)
                },
                new[] // Test 12: Extreme bottom left corner
                {
                    new Vector2(Window.position.width + SelectionDragger.panAreaWidth * 5, -SelectionDragger.panAreaWidth * 5),
                    new Vector2(maxDiagonalDistance, -maxDiagonalDistance)
                },
            };

            Vector2 res = Vector2.zero;

            int i = 0;
            foreach (Vector2[] data in testData)
            {
                res = GraphView.TestSelectionDragger.GetEffectivePanSpeed(data[0]);
                Assert.AreEqual(data[1], res, $"Test {i} failed because ({data[1].x:R},{data[1].y:R} != ({res.x:R}, {res.y:R})");
                i++;
            }

            yield return null;
        }
    }
}
