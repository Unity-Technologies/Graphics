using NUnit.Framework;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Removal of tasks and bindings. Both splice a data node out of the dependency graph, reconnecting its
    /// children to the surviving nodes upstream that write what those children read, matched at data-view
    /// granularity. Data nodes are named after their task, so a shape reads as writer -> middle -> child.
    /// </summary>
    [TestFixture]
    public class TaskGraphRemovalTests
    {
        [Test]
        public void RemoveTask_ReconnectsChildToSurvivingWriter()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x")
                .AddTask("W").Bind("W", "S", write: "x", dataNodeName: "W")
                .AddTask("M").Bind("M", "S", read: "x", write: "x", dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: "x", dataNodeName: "C");

            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "M" }));

            builder.RemoveTask("M");

            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "W" }));
        }

        [Test]
        public void RemoveTask_LeavesDisjointSubViewsUnconnected()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "alive", "position")
                .AddTask("W").Bind("W", "S", write: "alive", dataNodeName: "W")
                .AddTask("M").Bind("M", "S", read: "alive", write: "position", dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: "position", dataNodeName: "C");

            builder.RemoveTask("M");

            // W writes only 'alive', C reads only 'position'. Matching on the cached Read/Written bits would
            // connect them anyway, since those propagate up to the shared container root.
            Assert.That(builder.ParentNames("C"), Is.Empty);
        }

        [Test]
        public void RemoveTask_ReconnectsThroughContainingView()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x")
                .AddTask("W").Bind("W", "S", write: TestGraphBuilder.Root, dataNodeName: "W")
                .AddTask("M").Bind("M", "S", read: TestGraphBuilder.Root, write: "x", dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: "x", dataNodeName: "C");

            builder.RemoveTask("M");

            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "W" }));
        }

        [Test]
        public void RemoveTask_ConnectsToEveryDistinctWriter()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y", "z")
                .AddTask("Wx").Bind("Wx", "S", write: "x", dataNodeName: "Wx")
                .AddTask("Wy").Bind("Wy", "S", read: "x", write: "y", dataNodeName: "Wy")
                .AddTask("M").Bind("M", "S", read: "y", write: "z", dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: "x,y,z", dataNodeName: "C");

            builder.RemoveTask("M");

            // Wx is a transitive ancestor of Wy but the only writer of 'x', so pruning dominated candidates
            // would lose the edge saying Wx feeds C.
            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "Wx", "Wy" }));
        }

        [Test]
        public void RemoveTask_ConnectsToEveryWriterOfACoarseRead()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y")
                .AddTask("Wx").Bind("Wx", "S", write: "x", dataNodeName: "Wx")
                .AddTask("Wy").Bind("Wy", "S", read: "x", write: "y", dataNodeName: "Wy")
                .AddTask("M").Bind("M", "S", read: "y", write: TestGraphBuilder.Root, dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: TestGraphBuilder.Root, dataNodeName: "C");

            builder.RemoveTask("M");

            // C reads the container whole while Wy writes only 'y', so that match covers part of the read.
            // Consuming all of it there would leave 'x' unattributed and lose the edge from Wx — the shape
            // real tasks produce, declaring the root while their ancestors declare one attribute each.
            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "Wx", "Wy" }));
        }

        [Test]
        public void RemoveTask_StopsAtTheNearestWriterOfASubView()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y")
                .AddTask("Old").Bind("Old", "S", write: "x", dataNodeName: "Old")
                .AddTask("New").Bind("New", "S", read: "x", write: "x", dataNodeName: "New")
                .AddTask("M").Bind("M", "S", read: "x", write: "y", dataNodeName: "M")
                .AddTask("C").Bind("C", "S", read: TestGraphBuilder.Root, dataNodeName: "C");

            builder.RemoveTask("M");

            // Old and New both write 'x' and C's read of the root overlaps each of them, but keeping a read
            // alive through a partial match must not walk past the nearest writer of that part: New already
            // orders C after Old.
            Assert.That(builder.ParentNames("C"), Is.EquivalentTo(new[] { "New" }));
        }

        [Test]
        public void RemoveTask_WithSeveralDataNodes_ReconnectsEach()
        {
            var builder = new TestGraphBuilder()
                .AddData("S1", "x")
                .AddData("S2", "y")
                .AddTask("W1").Bind("W1", "S1", write: "x", dataNodeName: "W1")
                .AddTask("W2").Bind("W2", "S2", write: "y", dataNodeName: "W2")
                .AddTask("M")
                    .Bind("M", "S1", read: "x", write: "x", dataNodeName: "M.S1")
                    .Bind("M", "S2", read: "y", write: "y", dataNodeName: "M.S2")
                .AddTask("C1").Bind("C1", "S1", read: "x", dataNodeName: "C1")
                .AddTask("C2").Bind("C2", "S2", read: "y", dataNodeName: "C2");

            // Splicing the first data node reconnects a child, bumping the graph version while the remaining
            // data nodes are still being walked.
            Assert.That(() => builder.RemoveTask("M"), Throws.Nothing);

            Assert.That(builder.ParentNames("C1"), Is.EquivalentTo(new[] { "W1" }));
            Assert.That(builder.ParentNames("C2"), Is.EquivalentTo(new[] { "W2" }));
        }

        [Test]
        public void RemoveTask_WithSeveralDataNodesAndWriters_ReconnectsEach()
        {
            var builder = new TestGraphBuilder()
                .AddData("S1", "x", "y", "z")
                .AddData("S2", "x", "y", "z")
                .AddTask("W1a").Bind("W1a", "S1", write: "x", dataNodeName: "W1a")
                .AddTask("W1b").Bind("W1b", "S1", read: "x", write: "y", dataNodeName: "W1b")
                .AddTask("W2a").Bind("W2a", "S2", write: "x", dataNodeName: "W2a")
                .AddTask("W2b").Bind("W2b", "S2", read: "x", write: "y", dataNodeName: "W2b")
                .AddTask("M")
                    .Bind("M", "S1", read: "y", write: "z", dataNodeName: "M.S1")
                    .Bind("M", "S2", read: "y", write: "z", dataNodeName: "M.S2")
                .AddTask("C1").Bind("C1", "S1", read: "x,y,z", dataNodeName: "C1")
                .AddTask("C2").Bind("C2", "S2", read: "x,y,z", dataNodeName: "C2");

            builder.RemoveTask("M");

            Assert.That(builder.ParentNames("C1"), Is.EquivalentTo(new[] { "W1a", "W1b" }));
            Assert.That(builder.ParentNames("C2"), Is.EquivalentTo(new[] { "W2a", "W2b" }));
        }

        [Test]
        public void RemoveTask_RemovesTaskWithItsDataNodes()
        {
            var builder = new TestGraphBuilder()
                .AddData("S1", "x")
                .AddData("S2", "y")
                .AddTask("T")
                    .Bind("T", "S1", write: "x", dataNodeName: "T.S1")
                    .Bind("T", "S2", write: "y", dataNodeName: "T.S2");

            builder.RemoveTask("T");

            Assert.That(builder.IsTaskAlive("T"), Is.False);
            Assert.That(builder.IsDataNodeAlive("T.S1"), Is.False);
            Assert.That(builder.IsDataNodeAlive("T.S2"), Is.False);
        }

        [Test]
        public void RemoveTask_WithSeveralBindingsPerDataNode_RemovesThemAll()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y")
                .AddTask("W").Bind("W", "S", write: "x", dataNodeName: "W")
                .AddTask("T")
                    .Bind("T", "S", read: "x", dataNodeName: "T.read")
                    .Bind("T", "S", write: "y", dataNodeName: "T.write");

            var dataNodeId = builder.DataNodeIdOf("T.read");
            var viewX = builder.GetSubView("S", "x");

            // One data node per (task, container), so both bindings share it and are announced as removed
            // after it is already gone.
            Assert.That(() => builder.RemoveTask("T"), Throws.Nothing);

            Assert.That(builder.IsTaskAlive("T"), Is.False);
            Assert.That(builder.IsDataNodeAlive("T.read"), Is.False);
            Assert.That(builder.IsReadById(dataNodeId, viewX), Is.False);
            Assert.That(builder.IsTaskAlive("W"), Is.True);
            Assert.That(builder.IsWritten("W", "x"), Is.True);
        }

        [Test]
        public void UnbindData_RemovesDataNodeWithItsLastBinding()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x")
                .AddTask("T").Bind("T", "S", write: "x");

            builder.UnbindData("T", "S");

            Assert.That(builder.IsDataNodeAlive("S"), Is.False);
            Assert.That(builder.IsTaskAlive("T"), Is.True);
            Assert.That(builder.BindingCount("T"), Is.Zero);
        }

        [Test]
        public void UnbindData_KeepsDataNodeWithRemainingBindings_AndDropsOnlyTheUnboundUsage()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y")
                .AddTask("T")
                    .Bind("T", "S", write: "x", dataNodeName: "T.x")
                    .Bind("T", "S", write: "y", dataNodeName: "T.y");

            // One data node per (task, container), so both bindings share it.
            Assert.That(builder.GetDataNode("T.y").Id, Is.EqualTo(builder.GetDataNode("T.x").Id));
            Assert.That(builder.IsWritten("T.x", "x"), Is.True);
            Assert.That(builder.IsWritten("T.x", "y"), Is.True);

            builder.UnbindData("T", "T.y");

            Assert.That(builder.IsDataNodeAlive("T.x"), Is.True);
            Assert.That(builder.IsWritten("T.x", "x"), Is.True);
            Assert.That(builder.IsWritten("T.x", "y"), Is.False);
        }

        [Test]
        public void RemoveTask_ClearsTheUsageOfItsRemovedDataNodes()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x")
                .AddTask("W").Bind("W", "S", write: "x", dataNodeName: "W")
                .AddTask("T").Bind("T", "S", read: "x", dataNodeName: "T");

            var dataNodeId = builder.DataNodeIdOf("T");
            var viewX = builder.GetSubView("S", "x");
            Assert.That(builder.IsReadById(dataNodeId, viewX), Is.True);

            builder.RemoveTask("T");

            // The view outlives the node here, so a leaked flag would still be reachable by id.
            Assert.That(builder.IsViewAlive(viewX), Is.True);
            Assert.That(builder.IsReadById(dataNodeId, viewX), Is.False);
        }

        [Test]
        public void RemoveDataContainer_RemovesItsViewsNodesAndBindings()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x", "y")
                .AddTask("T").Bind("T", "S", read: "x", write: "y", dataNodeName: "T");

            var rootView = builder.ContainerViewId("S");
            var viewX = builder.GetSubView("S", "x");
            var viewY = builder.GetSubView("S", "y");

            builder.RemoveDataContainer("S");

            Assert.That(builder.IsContainerAlive("S"), Is.False);
            Assert.That(builder.IsViewAlive(rootView), Is.False);
            Assert.That(builder.IsViewAlive(viewX), Is.False);
            Assert.That(builder.IsViewAlive(viewY), Is.False);
            Assert.That(builder.IsDataNodeAlive("T"), Is.False);
            Assert.That(builder.IsTaskAlive("T"), Is.True, "only the container was removed.");
            Assert.That(builder.BindingCount("T"), Is.Zero);
        }

        [Test]
        public void RemoveDataContainer_KeepsTheTaskAndItsOtherContainers()
        {
            var builder = new TestGraphBuilder()
                .AddData("S1", "x")
                .AddData("S2", "y")
                .AddTask("T")
                    .Bind("T", "S1", write: "x", dataNodeName: "T.S1")
                    .Bind("T", "S2", write: "y", dataNodeName: "T.S2");

            builder.RemoveDataContainer("S1");

            Assert.That(builder.IsDataNodeAlive("T.S1"), Is.False);
            Assert.That(builder.IsDataNodeAlive("T.S2"), Is.True);
            Assert.That(builder.BindingCount("T"), Is.EqualTo(1));
            Assert.That(builder.IsWritten("T.S2", "y"), Is.True);
        }

        [Test]
        public void RemoveDataContainer_WithSeveralBindingsPerDataNode_KeepsTheTaskConsistent()
        {
            var builder = new TestGraphBuilder()
                .AddData("Doomed", "x", "y")
                .AddData("Kept", "z")
                .AddTask("T")
                    .Bind("T", "Doomed", read: "x", dataNodeName: "T.read")
                    .Bind("T", "Doomed", write: "y", dataNodeName: "T.write")
                    .Bind("T", "Kept", write: "z", dataNodeName: "T.kept");

            builder.RemoveDataContainer("Doomed");

            // The task outlives its data node here, so its binding list has to lose exactly the two bindings
            // that hung off the removed node — both announced once that node is already gone.
            Assert.That(builder.BindingCount("T"), Is.EqualTo(1));
            Assert.That(builder.IsDataNodeAlive("T.kept"), Is.True);
            Assert.That(builder.IsWritten("T.kept", "z"), Is.True);
        }

        [Test]
        public void RemoveDataContainer_LeavesOtherContainersIntact()
        {
            var builder = new TestGraphBuilder()
                .AddData("Doomed", "x")
                .AddData("Kept", "y")
                .AddTask("Wd").Bind("Wd", "Doomed", write: "x", dataNodeName: "Wd")
                .AddTask("Wk").Bind("Wk", "Kept", write: "y", dataNodeName: "Wk")
                .AddTask("Ck").Bind("Ck", "Kept", read: "y", dataNodeName: "Ck");

            var keptView = builder.ContainerViewId("Kept");
            builder.RemoveDataContainer("Doomed");

            Assert.That(builder.IsContainerAlive("Kept"), Is.True);
            Assert.That(builder.IsViewAlive(keptView), Is.True);
            Assert.That(builder.IsWritten("Wk", "y"), Is.True);
            Assert.That(builder.IsRead("Ck", "y"), Is.True);
            Assert.That(builder.ParentNames("Ck"), Is.EquivalentTo(new[] { "Wk" }));
        }

        [Test]
        public void RemoveDataContainer_LeavesTheGraphUsableAfterwards()
        {
            var builder = new TestGraphBuilder()
                .AddData("Doomed", "x", "y")
                .AddTask("T").Bind("T", "Doomed", read: "x", write: "y", dataNodeName: "T");

            builder.RemoveDataContainer("Doomed");

            // A view tree torn down parent-first would leave promoted roots in DataViewTrees and stale
            // sub-view keys, which a fresh container exercising the same caches would then trip over.
            builder.AddData("Fresh", "x", "y")
                .AddTask("U").Bind("U", "Fresh", read: "x", write: "y", dataNodeName: "U");

            Assert.That(builder.IsRead("U", "x"), Is.True);
            Assert.That(builder.IsWritten("U", "y"), Is.True);
            Assert.That(builder.IsWritten("U", "x"), Is.False);
            Assert.That(builder.GetSubView("Fresh", "x"), Is.Not.EqualTo(builder.GetSubView("Fresh", "y")));
        }

        [Test]
        public void UnbindData_IgnoresAnUnknownBindingKey()
        {
            var builder = new TestGraphBuilder()
                .AddData("S", "x")
                .AddTask("T").Bind("T", "S", write: "x");

            var versionBefore = builder.Graph.Version;

            builder.UnbindData("T", "nope");

            Assert.That(builder.Graph.Version, Is.EqualTo(versionBefore));
            Assert.That(builder.BindingCount("T"), Is.EqualTo(1));
        }
    }
}
