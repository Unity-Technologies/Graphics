using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
            node1 = m_Node1Model.GetView<Node>(GraphView);
            if (node1 != null)
                node1.viewDataKey = k_Key1;

            node2 = m_Node2Model.GetView<Node>(GraphView);
            if (node2 != null)
                node2.viewDataKey = k_Key2;

            node3 = m_Node3Model.GetView<Node>(GraphView);
            if (node3 != null)
                node3.viewDataKey = k_Key3;
        }

        [UnityTest]
        public IEnumerator CanUndoSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

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

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, m_Node1Model, m_Node3Model));

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }
    }
}
