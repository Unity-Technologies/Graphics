using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    class TracingTests
    {
        [Test]
        public void IndexFramesPerNodeTest()
        {
            TracingTimeline.FramesPerNodeCache cached = default;
            var stencil = new TestStencil();

            var n1 = NodeGuid(1);
            var n2 = NodeGuid(2);
            var n3 = NodeGuid(3);

            var graphTrace = new TestGraphTrace(0, 4, new Dictionary<int, SerializableGUID[]>
            {
                {0, new[] {n1, n2}}, // frame 0, nodes 1 and 3 are active
                {1, new[] {n1, n3}},
                {2, new[] {n3}},
                {3, new[] {n1}},
                {4, new[] {n1, n2}},
            });

            Dictionary<SerializableGUID, List<(int InclusiveStartFrame, int ExclusiveEndFrame)>> index;

            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 0, 2, 0, out bool invalidated);
            Assert.IsTrue(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 2) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 1) }, index[n2]);
            CollectionAssert.AreEqual(new List<(int, int)> { (1, 3) }, index[n3]);
            CheckGetDebuggingStepsCallCount(3);

            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 0, 2, 0, out invalidated);
            Assert.IsFalse(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 2) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 1) }, index[n2]);
            CollectionAssert.AreEqual(new List<(int, int)> { (1, 3) }, index[n3]);
            CheckGetDebuggingStepsCallCount(0);

            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 0, 3, 0, out invalidated);
            Assert.IsTrue(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 2), (3, 4) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 1) }, index[n2]);
            CollectionAssert.AreEqual(new List<(int, int)> { (1, 3) }, index[n3]);
            CheckGetDebuggingStepsCallCount(1);

            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 0, 4, 0, out invalidated);
            Assert.IsTrue(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 2), (3, 5) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (0, 1), (4, 5) }, index[n2]);
            CollectionAssert.AreEqual(new List<(int, int)> { (1, 3) }, index[n3]);
            CheckGetDebuggingStepsCallCount(1);

            // drop first frame
            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 1, 4, 0, out invalidated);
            Assert.IsTrue(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (/*0 not removed, should be 1 now*/ 0, 2), (3, 5) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (4, 5) }, index[n2]);
            CollectionAssert.AreEqual(new List<(int, int)> { (1, 3) }, index[n3]);
            CheckGetDebuggingStepsCallCount(0);

            index = TracingTimeline.IndexFramesPerNode(ref cached, stencil, graphTrace, 3, 4, 0, out invalidated);
            Assert.IsTrue(invalidated);
            CollectionAssert.AreEqual(new List<(int, int)> { (3, 5) }, index[n1]);
            CollectionAssert.AreEqual(new List<(int, int)> { (4, 5) }, index[n2]);
            CollectionAssert.IsEmpty(index[n3]); // still there.
            CheckGetDebuggingStepsCallCount(0);

            void CheckGetDebuggingStepsCallCount(int i)
            {
                Assert.AreEqual(i, TestFrameData.GetDebuggingStepsCallCount);
                TestFrameData.GetDebuggingStepsCallCount = 0;
            }
        }

        SerializableGUID NodeGuid(uint index) => new SerializableGUID(index, 0);

        internal class TestGraphTrace : IGraphTrace
        {
            private readonly int _firstFrame;
            private readonly int _lastFrame;

            public TestGraphTrace(int firstFrame, int lastFrame, Dictionary<int, SerializableGUID[]> nodesActivePerFrame)
            {
                _firstFrame = firstFrame;
                _lastFrame = lastFrame;

                AllFrames = Enumerable.Range(_firstFrame, _lastFrame - _firstFrame + 1)
                    .Select(i => MakeFrame(i, nodesActivePerFrame.TryGetValue(i, out var data) ? data : null)).ToList();
            }

            public IReadOnlyList<IFrameData> AllFrames { get; }

            private IFrameData MakeFrame(int frame, SerializableGUID[] activeNodes)
            {
                return new TestFrameData(frame, activeNodes);
            }
        }

        class TestFrameData : IFrameData
        {
            public static int GetDebuggingStepsCallCount;
            public int Frame { get; }
            private readonly SerializableGUID[] _activeNodes;
            private readonly TracingStep[] _steps;
            public TestFrameData(int frame, SerializableGUID[] activeNodes)
            {
                _activeNodes = activeNodes;
                Frame = frame;
                _steps = _activeNodes == null
                    ? new TracingStep[0]
                    : _activeNodes.Select(activeNode => TracingStep.ExecutedNode(new Type0FakeNodeModel { Guid = activeNode }, 0)).ToArray();
            }

            public IEnumerable<TracingStep> GetDebuggingSteps(Stencil stencil)
            {
                GetDebuggingStepsCallCount++;
                return _steps;
            }
        }

        class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider() => null;
            public override Type GetConstantNodeValueType(TypeHandle typeHandle)
            {
                return TypeToConstantMapper.GetConstantNodeType(typeHandle);
            }

            /// <inheritdoc />
            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            {
                throw new NotImplementedException();
            }
        }
    }
}
