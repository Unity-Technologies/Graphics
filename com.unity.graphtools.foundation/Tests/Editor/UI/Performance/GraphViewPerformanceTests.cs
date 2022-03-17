using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI.Performance
{
    public class GraphViewPerformanceTests : BaseUIFixture
    {
        const int k_WarmupCount = 3;
        const int k_MeasurementCount = 10;

        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        IEnumerable<Type0FakeNodeModel> CreateNodesAndEdges(int count)
        {
            /*
             * Number of nodes: 2*count
             * Number of edges: 3*count
             */

            count /= 2;

            var fromNodes = new Type0FakeNodeModel[count];
            var toNodes = new Type0FakeNodeModel[count];

            // It is more efficient to create all nodes first, then to create the edges.
            // This avoids dirtying the port-edge index before each edge creation.
            for (var i = 0; i < count; i++)
            {
                fromNodes[i] = GraphModel.CreateNode<Type0FakeNodeModel>($"Node {i}", new Vector2(100, i * 200));
                fromNodes[i].DefineNode();

                toNodes[i] = GraphModel.CreateNode<Type0FakeNodeModel>($"Node {i}", new Vector2(100, i * 200));
                toNodes[i].DefineNode();
            }

            for (var i = 0; i < count; i++)
            {
                var inputs = toNodes[i].InputsByDisplayOrder.ToList();
                var outputs = fromNodes[i].OutputsByDisplayOrder.ToList();

                // Create edge between execution ports.
                GraphModel.CreateEdge(inputs[0], outputs[0]);

                // Create edges from the first output data port to each input data port.
                GraphModel.CreateEdge(inputs[1], outputs[1]);
                GraphModel.CreateEdge(inputs[2], outputs[1]);
            }

            return fromNodes.Concat(toNodes);
        }

        [Test, Performance]
        [TestCase(20)]
        [TestCase(40, Explicit = true, Reason = "Too slow. See GTF-635")]
        [TestCase(100, Explicit = true, Reason = "Too slow. See GTF-635")]
        [TestCase(1000, Explicit = true, Reason = "Too slow for automated testing")]
        public void GraphViewCompleteUpdateNodesAndEdges(int count)
        {
            CreateNodesAndEdges(count);

            Measure.Method(() => GraphView.BuildUI())
                .WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(20)]
        [TestCase(40)]
        [TestCase(100, Explicit = true, Reason = "Too slow for automated testing")]
        public void GraphViewCompleteUpdateStickyNotes(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var stickyNote = GraphModel.CreateStickyNote(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                stickyNote.Title = $"Sticky {i}";
                stickyNote.Contents = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris";
            }

            Measure.Method(() => GraphView.BuildUI())
                .WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }


        [Test, Performance]
        [TestCase(20)]
        [TestCase(40)]
        [TestCase(100, Explicit = true, Reason = "Too slow for automated testing")]
        public void GraphViewCompleteUpdatePlacemats(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var placemat = GraphModel.CreatePlacemat(new Rect(new Vector2(100, i * 400), new Vector2( 300, 300)));
                placemat.Title = $"Placemat {i}";
            }

            Measure.Method(() => GraphView.BuildUI())
                .WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(1000)]
        public void GraphViewUpdateAfterMoveSingleNode(int count)
        {
            var firstNode = CreateNodesAndEdges(count).First();

            void MoveNode()
            {
                // Do not use commands because we want to time the update, not the undo system.
                var graphModelState = GraphView.GraphViewModel.GraphModelState;
                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    firstNode.Move(new Vector2(100, 100));
                    graphUpdater.MarkChanged(firstNode);
                }

                GraphTool.Update();
            }

            Measure.Method(MoveNode)
                .WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(1000)]
        public void GraphViewUpdateAfterMoveMultipleNode(int count)
        {
            var nodes = CreateNodesAndEdges(count).Take(count / 2);

            void MoveNode()
            {
                // Do not use commands because we want to time the update, not the undo system.
                var graphModelState = GraphView.GraphViewModel.GraphModelState;
                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    foreach (var node in nodes)
                    {
                        node.Move(new Vector2(100, 100));
                        graphUpdater.MarkChanged(node);
                    }
                }

                GraphTool.Update();
            }

            Measure.Method(MoveNode)
                .WarmupCount(k_WarmupCount)
                .MeasurementCount(k_MeasurementCount)
                .Run();
        }
    }
}
