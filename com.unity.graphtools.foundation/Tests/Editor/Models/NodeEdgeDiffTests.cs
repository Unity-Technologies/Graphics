using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class NodeEdgeDiffTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void NodeEdgeDiffCorrectlyReportsDeletedEdges()
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var edge1 = GraphModel.CreateEdge(node2.Input0, node1.Output0);
            GraphModel.CreateEdge(node2.Input1, node1.Output0);

            var diff1In = new NodeEdgeDiff(node1, PortDirection.Input);
            var diff1Out = new NodeEdgeDiff(node1, PortDirection.Output);
            var diff1Both = new NodeEdgeDiff(node1, PortDirection.None);
            var diff2In = new NodeEdgeDiff(node2, PortDirection.Input);
            var diff2Out = new NodeEdgeDiff(node2, PortDirection.Output);
            var diff2Both = new NodeEdgeDiff(node2, PortDirection.None);

            GraphModel.DeleteEdges(new[] { edge1 });

            Assert.AreEqual(0, diff1In.GetDeletedEdges().Count());
            Assert.AreEqual(1, diff1Out.GetDeletedEdges().Count());
            Assert.AreEqual(1, diff1Both.GetDeletedEdges().Count());
            Assert.AreEqual(1, diff2In.GetDeletedEdges().Count());
            Assert.AreEqual(0, diff2Out.GetDeletedEdges().Count());
            Assert.AreEqual(1, diff2Both.GetDeletedEdges().Count());

            Assert.Contains(edge1, diff1Out.GetDeletedEdges().ToList());
            Assert.Contains(edge1, diff1Both.GetDeletedEdges().ToList());
            Assert.Contains(edge1, diff2In.GetDeletedEdges().ToList());
            Assert.Contains(edge1, diff2Both.GetDeletedEdges().ToList());
        }

        [Test]
        public void NodeEdgeDiffCorrectlyReportsAddedEdges()
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>();
            GraphModel.CreateEdge(node2.Input0, node1.Output0);

            var diff1In = new NodeEdgeDiff(node1, PortDirection.Input);
            var diff1Out = new NodeEdgeDiff(node1, PortDirection.Output);
            var diff1Both = new NodeEdgeDiff(node1, PortDirection.None);
            var diff2In = new NodeEdgeDiff(node2, PortDirection.Input);
            var diff2Out = new NodeEdgeDiff(node2, PortDirection.Output);
            var diff2Both = new NodeEdgeDiff(node2, PortDirection.None);

            var edge2 = GraphModel.CreateEdge(node2.Input1, node1.Output0);

            Assert.AreEqual(0, diff1In.GetAddedEdges().Count());
            Assert.AreEqual(1, diff1Out.GetAddedEdges().Count());
            Assert.AreEqual(1, diff1Both.GetAddedEdges().Count());
            Assert.AreEqual(1, diff2In.GetAddedEdges().Count());
            Assert.AreEqual(0, diff2Out.GetAddedEdges().Count());
            Assert.AreEqual(1, diff2Both.GetAddedEdges().Count());

            Assert.Contains(edge2, diff1Out.GetAddedEdges().ToList());
            Assert.Contains(edge2, diff1Both.GetAddedEdges().ToList());
            Assert.Contains(edge2, diff2In.GetAddedEdges().ToList());
            Assert.Contains(edge2, diff2Both.GetAddedEdges().ToList());
        }
    }
}
