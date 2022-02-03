using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementSelectTests : GraphViewTester
    {
        IONodeModel m_Node1;
        IONodeModel m_Node2;
        IONodeModel m_Node3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(10, 30));
            m_Node2 = CreateNode("Node 2", new Vector2(270, 30));
            m_Node3 = CreateNode("Node 3", new Vector2(400, 30)); // overlaps m_Node2
        }

        void GetUI(out Node node1, out Node node2, out Node node3)
        {
            node1 = m_Node1.GetUI<Node>(graphView);
            node2 = m_Node2.GetUI<Node>(graphView);
            node3 = m_Node3.GetUI<Node>(graphView);
        }

        Rect RectAroundNodes(Node node1, Node node2, Node node3)
        {
            // Generate a rectangle to select all the elements
            Rect rectangle = RectUtils.Encompass(RectUtils.Encompass(node1.worldBound, node2.worldBound), node3.worldBound);
            rectangle = RectUtils.Inflate(rectangle, 1, 1, 1, 1);
            return rectangle;
        }

        [UnityTest]
        public IEnumerator ElementCanBeSelected()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            helpers.Click(node1);

            yield return null;

            Assert.True(node1?.IsSelected());
            Assert.False(node2?.IsSelected());
            Assert.False(node3?.IsSelected());
        }

        [UnityTest]
        public IEnumerator ChangingElementLayerDoesntAffectSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            helpers.Click(node1);

            yield return null;

            Assert.True(node1?.IsSelected());
            Assert.False(node2?.IsSelected());
            Assert.False(node3?.IsSelected());

            node1.Layer += 100;
            yield return null;

            Assert.True(node1?.IsSelected());
            Assert.False(node2?.IsSelected());
            Assert.False(node3?.IsSelected());

            node1.Layer -= 200;
            yield return null;

            Assert.True(node1?.IsSelected());
            Assert.False(node2?.IsSelected());
            Assert.False(node3?.IsSelected());
        }

        [UnityTest]
        public IEnumerator SelectingNewElementUnselectsPreviousOne()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.False(node3.IsSelected());

            // Select elem 2. All other elems should be unselected.
            helpers.Click(node2);

            yield return null;

            Assert.False(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.False(node3.IsSelected());
        }

        EventModifiers CommandOrControl => Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control;

        [UnityTest]
        public IEnumerator SelectingNewElementWithCommandAddsToSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(node2, eventModifiers: CommandOrControl);

            yield return null;

            Assert.True(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.False(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator SelectingSelectedElementWithCommandModifierRemovesFromSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(node2, eventModifiers: CommandOrControl);

            yield return null;

            // Select elem 1 with control. Only 2 should be selected
            helpers.Click(node1, eventModifiers: CommandOrControl);

            yield return null;

            Assert.False(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.False(node3.IsSelected());
        }

        // Taken from internal QuadTree utility
        static bool Intersection(Rect r1, Rect r2, out Rect intersection)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
            {
                intersection = new Rect(0, 0, 0, 0);
                return false;
            }

            float left = Mathf.Max(r1.xMin, r2.xMin);
            float top = Mathf.Max(r1.yMin, r2.yMin);

            float right = Mathf.Min(r1.xMax, r2.xMax);
            float bottom = Mathf.Min(r1.yMax, r2.yMax);
            intersection = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        [UnityTest]
        public IEnumerator ClickOnTwoOverlappingElementsSelectsTopOne()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Find the intersection between those two nodes and click right in the middle
            Rect intersection;
            Assert.IsTrue(Intersection(node2.worldBound, node3.worldBound, out intersection), "Expected rectangles to intersect");

            helpers.Click(intersection.center);

            yield return null;

            Assert.False(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.True(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWithActionKeyWorks()
        {
            graphView.DispatchFrameAllCommand();
            yield return null;

            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1));
            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.False(node3.IsSelected());
            yield return null;

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            // Reselect all.
            helpers.DragTo(rectangle.min, rectangle.max, eventModifiers: CommandOrControl);
            yield return null;

            GetUI(out node1, out node2, out node3);
            Assert.False(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator FreehandSelectionWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift, steps: 10);

            yield return null;

            Assert.True(node1.IsSelected());
            Assert.True(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator FreehandDeleteWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift | EventModifiers.Alt, steps: 10);

            yield return null;

            // After manipulation we should have only zero elements left.
            var allUIs = new List<ModelUI>();
            GraphModel.GraphElementModels.GetAllUIsInList(graphView, null, allUIs);
            Assert.IsEmpty(allUIs);
        }

        [Test]
        public void AddingElementToSelectionTwiceDoesNotAddTheSecondTime()
        {
            graphView.RebuildUI();
            GetUI(out var node1, out var node2, out _);

            Assert.AreEqual(0, graphView.GetSelection().Count);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1));
            Assert.AreEqual(1, graphView.GetSelection().Count);

            // Add same element again, should have no impact on selection
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1));
            Assert.AreEqual(1, graphView.GetSelection().Count);

            // Add other element
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node2));
            Assert.AreEqual(2, graphView.GetSelection().Count);
        }

        [Test]
        public void RemovingElementFromSelectionTwiceDoesThrowException()
        {
            graphView.RebuildUI();
            GetUI(out var node1, out var node2, out _);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1, m_Node2));
            Assert.AreEqual(2, graphView.GetSelection().Count);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, m_Node2));
            Assert.AreEqual(1, graphView.GetSelection().Count);

            // Remove the same item again, should have no impact on selection
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, m_Node2));
            Assert.AreEqual(1, graphView.GetSelection().Count);

            // Remove other element
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, m_Node1));
            Assert.AreEqual(0, graphView.GetSelection().Count);
        }
    }
}
