using NUnit.Framework;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Data-side traversal behaviour with no task counterpart, so not parameterisable over
    /// <see cref="NodeKind"/> in <see cref="GraphTraverserTests"/>: data nodes are labelled by their
    /// container rather than their task, and a task may own several of them, so the data graph is not
    /// in general a copy of the task graph. The shared shapes avoid both cases by binding one task to
    /// one same-named container — an assumption <see cref="ShorthandShapes_MirrorTheTaskGraph"/> guards.
    /// </summary>
    [TestFixture]
    public class GraphTraverserDataTests
    {
        [Test]
        public void ShorthandShapes_MirrorTheTaskGraph()
        {
            var builder = TestGraphBuilder.Chain("A", "B", "C");

            foreach (var name in new[] { "A", "B", "C" })
            {
                var taskNode = builder.GetTaskNode(name);
                Assert.That(taskNode.DataNodes.Count, Is.EqualTo(1), $"Task '{name}' should own one data node.");
                Assert.That(taskNode.DataNodes[0].Id, Is.EqualTo(builder.GetDataNode(name).Id));
                // The shared tests' labels come from the container, not the task.
                Assert.That(builder.GetDataNode(name).DataContainer.Name, Is.EqualTo(name));
            }
        }

        [Test]
        public void ComplexDag_DataAndTaskTraversalsAgree()
        {
            var data = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), NodeKind.Data);
            var task = data.With(NodeKind.Task);

            // The shared tests assert each kind against a literal; this asserts them against each
            // other, so a divergence is reported as one.
            Assert.That(data.Downwards("A"), Is.EqualTo(task.Downwards("A")));
            Assert.That(data.Upwards("H"), Is.EqualTo(task.Upwards("H")));
            Assert.That(data.ParentsFirst(), Is.EqualTo(task.ParentsFirst()));
            Assert.That(data.ChildrenFirst(), Is.EqualTo(task.ChildrenFirst()));
        }

        [Test]
        public void DataNodeNames_AreIndependentOfTaskNames()
        {
            var builder = new TestGraphBuilder()
                .AddTask("Producer").AddTask("Consumer")
                .AddData("bufferIn").AddData("bufferOut")
                .Bind("Producer", "bufferIn")
                .Bind("Consumer", "bufferOut")
                .DataEdge("bufferIn", "bufferOut");

            var data = new NodeKindTraversal(builder, NodeKind.Data);
            var task = data.With(NodeKind.Task);

            // The same edge under two unrelated sets of names.
            var dataVisited = data.Downwards("bufferIn");
            var taskVisited = task.Downwards("Producer");

            Assert.That(dataVisited, Is.EqualTo(new[] { "bufferIn", "bufferOut" }));
            Assert.That(taskVisited, Is.EqualTo(new[] { "Producer", "Consumer" }));
            data.AssertParentsFirst(dataVisited);
            task.AssertParentsFirst(taskVisited);
        }

        [Test]
        public void ATaskMayOwnSeveralDataNodes_OnePerBoundContainer()
        {
            var builder = new TestGraphBuilder()
                .AddTask("T")
                .AddData("x").AddData("y")
                .Bind("T", "x")
                .Bind("T", "y");

            Assert.That(builder.GetTaskNode("T").DataNodes.Count, Is.EqualTo(2));
            Assert.That(builder.GetDataNode("x").DataContainer.Name, Is.EqualTo("x"));
            Assert.That(builder.GetDataNode("y").DataContainer.Name, Is.EqualTo("y"));

            var data = new NodeKindTraversal(builder, NodeKind.Data);

            // Neither has a dependency, so the data graph has two independent nodes and the task
            // graph one.
            Assert.That(data.Roots(), Is.EquivalentTo(new[] { "x", "y" }));
            Assert.That(data.With(NodeKind.Task).Roots(), Is.EqualTo(new[] { "T" }));
        }

        [Test]
        public void SeveralDataNodesPerTask_ProduceASingleDerivedTaskEdge()
        {
            // Two containers flowing Producer -> Consumer: two data edges, one task edge.
            var builder = new TestGraphBuilder()
                .AddTask("Producer").AddTask("Consumer")
                .AddData("first").AddData("second")
                .Bind("Producer", "first", dataNodeName: "first@Producer")
                .Bind("Producer", "second", dataNodeName: "second@Producer")
                .Bind("Consumer", "first", dataNodeName: "first@Consumer")
                .Bind("Consumer", "second", dataNodeName: "second@Consumer")
                .DataEdge("first@Producer", "first@Consumer")
                .DataEdge("second@Producer", "second@Consumer");

            Assert.That(builder.GetTaskNode("Producer").DataNodes.Count, Is.EqualTo(2));
            Assert.That(builder.GetTaskNode("Consumer").DataNodes.Count, Is.EqualTo(2));

            var data = new NodeKindTraversal(builder, NodeKind.Data);

            // The derived edge is reference-counted, so it exists exactly once.
            Assert.That(builder.GetTaskNode("Consumer").Parents.Count, Is.EqualTo(1));
            Assert.That(data.With(NodeKind.Task).Downwards("Producer"),
                Is.EqualTo(new[] { "Producer", "Consumer" }));

            // Labelled by container, so each pair reads as the same name.
            Assert.That(data.Downwards("first@Producer"), Is.EqualTo(new[] { "first", "first" }));
            Assert.That(data.ParentsFirst(), Is.EqualTo(new[] { "first", "second", "first", "second" }));
        }
    }
}
