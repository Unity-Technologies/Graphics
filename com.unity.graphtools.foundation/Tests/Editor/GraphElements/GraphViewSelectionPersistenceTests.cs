using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewSelectionPersistenceTests : GraphViewTester
    {
        public GraphViewSelectionPersistenceTests() : base(enablePersistence: true) {}

        const string k_Key1 = "node1";
        const string k_Key2 = "node2";
        const string k_Key3 = "node3";

        INodeModel m_Node1Model;
        INodeModel m_Node2Model;
        INodeModel m_Node3Model;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // When using the EnterPlayMode yield instruction, the SetUp() of the test is ran again
            // In this case, we skip this to be in control of when nodes are created
            if (EditorApplication.isPlaying)
                return;

            m_Node1Model = CreateNode(k_Key1, new Vector2(200, 200));
            m_Node2Model = CreateNode(k_Key2, new Vector2(400, 400));
            m_Node3Model = CreateNode(k_Key3, new Vector2(600, 600));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            Undo.ClearAll();
        }

        void GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3)
        {
            node1 = m_Node1Model.GetUI<Node>(graphView);
            if (node1 != null)
                node1.viewDataKey = k_Key1;

            node2 = m_Node2Model.GetUI<Node>(graphView);
            if (node2 != null)
                node2.viewDataKey = k_Key2;

            node3 = m_Node3Model.GetUI<Node>(graphView);
            if (node3 != null)
                node3.viewDataKey = k_Key3;
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesAfterPersistence()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            // Add two nodes to selection.
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesBeforePersistence()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            m_Node1Model = CreateNode(k_Key1, new Vector2(200, 200));
            m_Node2Model = CreateNode(k_Key2, new Vector2(400, 400));
            m_Node3Model = CreateNode(k_Key3, new Vector2(600, 600));

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator CanUndoSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            Undo.PerformUndo();

            Assert.False(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.False(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator CanRedoSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator CanRedoSelectionAndEnterPlayMode()
        {
            // Note: this somewhat complex use case ensure that selection for redo
            // and persisted selection are kep in sync
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());

            // Allow 1 frame to let the persistence be saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            m_Node1Model = CreateNode(k_Key1, new Vector2(200, 200));
            m_Node2Model = CreateNode(k_Key2, new Vector2(400, 400));
            m_Node3Model = CreateNode(k_Key3, new Vector2(600, 600));

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsBeforeAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, graphView);

                var inSection = new BlackboardSection();
                inSection.SetupBuildAndUpdate(null, graphView);
                inSection.name = "Section 1";
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, row.Model));

                Assert.True(row.IsSelected());
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add field to blackboard first then add blackboard to graphview.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, graphView);

                var inSection = new BlackboardSection();
                inSection.SetupBuildAndUpdate(null, graphView);
                inSection.name = "Section 1";
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                Assert.True(row.IsSelected());
            }
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsAfterAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, graphView);

                var inSection = new BlackboardSection();
                inSection.SetupBuildAndUpdate(null, graphView);
                inSection.name = "Section 1";
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, row.Model));

                Assert.True(row.IsSelected());
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add blackboard to graphview first then add field to blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, graphView);

                graphView.AddElement(blackboard);

                var inSection = new BlackboardSection();
                inSection.SetupBuildAndUpdate(null, graphView);
                inSection.name = "Section 1";
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                Assert.True(row.IsSelected());
            }
        }
    }
}
