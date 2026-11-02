using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class GradientUndoTests
    {
        // One snapshot per session rather than per change event is part of the contract.
        class CountingGraphObject : GraphObject
        {
            public int registerCount { get; private set; }

            public override void RegisterCompleteObjectUndo(string actionName)
            {
                registerCount++;
                base.RegisterCompleteObjectUndo(actionName);
            }
        }

        CountingGraphObject m_GraphObject;
        GradientObject m_GradientObject;

        static Gradient CreateGradient(float firstKeyTime)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.red, firstKeyTime), new GradientColorKey(Color.blue, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        static Gradient CopyOf(Gradient other)
        {
            var copy = new Gradient();
            copy.SetKeys(other.colorKeys, other.alphaKeys);
            copy.mode = other.mode;
            return copy;
        }

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
            m_GraphObject = ScriptableObject.CreateInstance<CountingGraphObject>();
            m_GraphObject.hideFlags = HideFlags.HideAndDontSave;
            var graphData = new GraphData();
            // ReplaceWith reads vertexContext/fragmentContext, which only exist once contexts are added.
            graphData.AddContexts();
            graphData.InitializeOutputs(null, null);
            graphData.AddCategory(CategoryData.DefaultCategory());
            m_GraphObject.graph = graphData;
            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.hideFlags = HideFlags.HideAndDontSave;
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            Object.DestroyImmediate(m_GradientObject);
            Object.DestroyImmediate(m_GraphObject);
        }

        // Mirrors how MaterialGraphEditWindow.Update applies a restored snapshot to the live graph.
        void PumpUndoRedo()
        {
            if (m_GraphObject.wasUndoRedoPerformed)
                m_GraphObject.HandleUndoRedo();
        }

        // The transient object carries the session's intermediate edits, as it does in a real control.
        void RecordLiveGradientEdit()
        {
            Undo.RegisterCompleteObjectUndo(m_GradientObject, "Modify Gradient Stop");
        }

        GradientNode FindGradientNode()
        {
            return m_GraphObject.graph.GetNodes<GradientNode>().FirstOrDefault();
        }

        GradientNode AddGradientNode()
        {
            Undo.IncrementCurrentGroup();
            m_GraphObject.RegisterCompleteObjectUndo("Add Node");
            m_GraphObject.graph.AddNode(new GradientNode());
            return FindGradientNode();
        }

        GradientUndoSession CreateSession(GradientNode node, bool canRecord = true)
        {
            return new GradientUndoSession(
                () => node.gradient,
                value => node.gradient = value,
                () => m_GraphObject.RegisterCompleteObjectUndo("Modify Gradient"),
                () => canRecord);
        }

        [Test]
        public void InSessionUndo_DoesNotTouchGraph()
        {
            var node = AddGradientNode();
            var session = CreateSession(node);

            Undo.IncrementCurrentGroup();
            session.BeginChange();
            RecordLiveGradientEdit();
            node.gradient = CreateGradient(0.25f);

            Undo.PerformUndo();

            Assert.IsFalse(m_GraphObject.wasUndoRedoPerformed,
                "An in-session undo must not restore the graph, or the open picker is torn down.");
        }

        [Test]
        public void Session_RecordsOneGraphSnapshot_RegardlessOfChangeCount()
        {
            var node = AddGradientNode();
            var session = CreateSession(node);
            var snapshotsBefore = m_GraphObject.registerCount;

            Undo.IncrementCurrentGroup();
            for (var i = 1; i <= 5; i++)
            {
                session.BeginChange();
                RecordLiveGradientEdit();
                node.gradient = CreateGradient(i * 0.1f);
            }
            session.RecordNetChange();

            Assert.AreEqual(snapshotsBefore + 1, m_GraphObject.registerCount,
                "Dragging a gradient stop must cost one whole-graph snapshot per session, not one per change event.");
        }

        [Test]
        public void SessionEndingAtItsStartValue_RecordsNothing()
        {
            var node = AddGradientNode();
            var session = CreateSession(node);
            var original = CopyOf(node.gradient);
            var snapshotsBefore = m_GraphObject.registerCount;

            Undo.IncrementCurrentGroup();
            session.BeginChange();
            node.gradient = CreateGradient(0.25f);
            node.gradient = original;
            session.RecordNetChange();

            Assert.AreEqual(snapshotsBefore, m_GraphObject.registerCount,
                "A session with no net change must not put anything on the graph's undo stack.");
        }

        // UUM-148459.
        [Test]
        public void NetChange_IsRedone_AfterUndoingAndRedoingNodeCreation()
        {
            var node = AddGradientNode();
            var session = CreateSession(node);
            var preSession = CopyOf(node.gradient);

            // One session, framed the way GradientField frames a picker session.
            Undo.IncrementCurrentGroup();
            var pickerGroup = Undo.GetCurrentGroup();
            session.BeginChange();
            RecordLiveGradientEdit();
            node.gradient = CreateGradient(0.10f);
            session.BeginChange();
            RecordLiveGradientEdit();
            node.gradient = CreateGradient(0.25f);
            var netChange = CopyOf(node.gradient);

            session.RecordNetChange();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Modify Gradient");
            Undo.CollapseUndoOperations(pickerGroup);

            Undo.PerformUndo();
            PumpUndoRedo();
            Assert.IsTrue(GradientUtil.CheckEquivalency(preSession, FindGradientNode().gradient),
                "Undoing the session must restore the gradient the node started with.");

            Undo.PerformUndo();
            PumpUndoRedo();
            Assert.IsNull(FindGradientNode(), "Undoing again must remove the node.");

            Undo.PerformRedo();
            PumpUndoRedo();
            Assert.IsNotNull(FindGradientNode(), "Redo must bring the node back.");

            Undo.PerformRedo();
            PumpUndoRedo();
            Assert.IsTrue(GradientUtil.CheckEquivalency(netChange, FindGradientNode().gradient),
                "Redoing past the node's creation must restore the gradient edit, not the node default.");
        }

        // A control torn down with the picker still open must still get its net change onto the stack.
        [Test]
        public void RecordNetChange_FromDetach_StillRegistersTheSession()
        {
            var node = AddGradientNode();
            var session = CreateSession(node);
            var preSession = CopyOf(node.gradient);
            var snapshotsBefore = m_GraphObject.registerCount;

            Undo.IncrementCurrentGroup();
            session.BeginChange();
            RecordLiveGradientEdit();
            node.gradient = CreateGradient(0.25f);

            // What OnDetachFromPanel now does before unsubscribing.
            session.RecordNetChange();

            Assert.AreEqual(snapshotsBefore + 1, m_GraphObject.registerCount);

            Undo.PerformUndo();
            PumpUndoRedo();
            Assert.IsTrue(GradientUtil.CheckEquivalency(preSession, FindGradientNode().gradient));
        }

        // Recording during a graph rebuild would snapshot mid-replace, so it is skipped.
        [Test]
        public void RecordNetChange_SkipsWhenTheGraphCannotBeRecorded()
        {
            var node = AddGradientNode();
            var session = CreateSession(node, canRecord: false);
            var snapshotsBefore = m_GraphObject.registerCount;

            Undo.IncrementCurrentGroup();
            session.BeginChange();
            node.gradient = CreateGradient(0.25f);
            session.RecordNetChange();

            Assert.AreEqual(snapshotsBefore, m_GraphObject.registerCount);
        }

        // The baseline must not survive a skipped record, or the next session would revert to a stale value.
        [Test]
        public void SkippedRecord_DoesNotLeaveAStaleBaseline()
        {
            var node = AddGradientNode();
            var session = CreateSession(node, canRecord: false);

            Undo.IncrementCurrentGroup();
            session.BeginChange();
            node.gradient = CreateGradient(0.25f);
            session.RecordNetChange();

            var snapshotsAfterSkip = m_GraphObject.registerCount;
            session.RecordNetChange();

            Assert.AreEqual(snapshotsAfterSkip, m_GraphObject.registerCount,
                "A second record with no intervening change must do nothing.");
        }

        // Pins why the net change is recorded at close rather than before the edits.
        [Test]
        public void SnapshotTakenBeforeTheEdits_LosesTheRedoState()
        {
            AddGradientNode();

            Undo.IncrementCurrentGroup();
            m_GraphObject.RegisterCompleteObjectUndo("Modify Gradient");
            Undo.IncrementCurrentGroup();
            FindGradientNode().gradient = CreateGradient(0.25f);

            Undo.PerformUndo();
            PumpUndoRedo();
            Undo.PerformRedo();
            PumpUndoRedo();

            Assert.AreEqual(0f, FindGradientNode().gradient.colorKeys[0].time, 1e-4f,
                "Undo.IncrementCurrentGroup flushes tracked objects, so a snapshot taken before the edits "
                + "leaves redo with the pre-edit gradient. If this starts failing, that behaviour changed.");
        }
    }
}
