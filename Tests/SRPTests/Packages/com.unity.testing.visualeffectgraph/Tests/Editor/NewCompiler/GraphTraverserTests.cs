using System;
using NUnit.Framework;
using Unity.GraphCommon.LowLevel.Editor;
using Direction = Unity.GraphCommon.LowLevel.Editor.GraphTraverser.Direction;
using TraversalMethod = Unity.GraphCommon.LowLevel.Editor.GraphTraverser.TraversalMethod;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Tests for <see cref="GraphTraverser"/>. Task and data traversal share one generic core plus a
    /// per-kind adapter, so every shape test is parameterised over <see cref="NodeKind"/>: the
    /// algorithm is asserted once and each adapter in turn, keeping the two kinds symmetrical by
    /// construction. Kind-specific tests live in <see cref="GraphTraverserDataTests"/>.
    /// </summary>
    /// <remarks>
    /// Child links are in declaration order and DepthFirstPre pushes them reversed onto a LIFO deque,
    /// so the first-declared child is always visited first and the expected sequences below are exact
    /// rather than merely valid orderings.
    /// </remarks>
    [TestFixture]
    public class GraphTraverserTests
    {
        // --- Depth-first pre-order ---------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Downwards_Chain_VisitsInOrder(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Chain("A", "B", "C"), kind);

            Assert.That(nav.Downwards("A"), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Upwards_Chain_VisitsInReverseOrder(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Chain("A", "B", "C"), kind);

            Assert.That(nav.Upwards("C"), Is.EqualTo(new[] { "C", "B", "A" }));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Downwards_Tree_DescendsFirstBranchBeforeSecond(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Tree(), kind);

            var visited = nav.Downwards("A");

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "D", "E", "C", "F" }));
            nav.AssertParentsFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Downwards_Diamond_VisitsSharedDescendantOnce(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.Downwards("A");

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "D", "C" }));
            Assert.That(visited, Is.Unique);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Downwards_ComplexDag_VisitsOnlyNodesReachableDownwards(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.Downwards("A");

            // B and E are unreachable downwards from A.
            Assert.That(visited, Is.EqualTo(new[] { "A", "C", "F", "H", "D", "G" }));
            Assert.That(visited, Is.Unique);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Upwards_Diamond_FromLeaf_ReachesRootOnce(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.Upwards("D");

            Assert.That(visited, Is.EqualTo(new[] { "D", "B", "A", "C" }));
            Assert.That(visited, Is.Unique);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Upwards_ComplexDag_FromLeaf_ReachesEveryNode(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.Upwards("H");

            Assert.That(visited, Is.EqualTo(new[] { "H", "F", "C", "A", "D", "B", "G", "E" }));
            Assert.That(visited, Is.Unique);
        }

        // --- Breadth-first -----------------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void BreadthFirst_Tree_VisitsLevelByLevel(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Tree(), kind);

            var visited = nav.DownwardsBreadthFirst("A");

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "C", "D", "E", "F" }));
            nav.AssertParentsFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void BreadthFirst_Diamond_VisitsSharedDescendantOnce(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.DownwardsBreadthFirst("A");

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "C", "D" }));
            Assert.That(visited, Is.Unique);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void BreadthFirst_ComplexDag_VisitsLevelByLevel(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.DownwardsBreadthFirst("A");

            Assert.That(visited, Is.EqualTo(new[] { "A", "C", "D", "F", "G", "H" }));
            Assert.That(visited, Is.Unique);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void BreadthFirstUpwards_Diamond_FromLeaf_VisitsLevelByLevel(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.UpwardsBreadthFirst("D");

            // Both mid-level parents precede the root, unlike depth-first.
            Assert.That(visited, Is.EqualTo(new[] { "D", "B", "C", "A" }));
            Assert.That(visited, Is.Unique);
        }

        // --- Depth-first post-order --------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void PostOrder_Diamond_YieldsSharedDescendantBeforeBothParents(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.PostOrder("A");

            Assert.That(visited, Is.EqualTo(new[] { "D", "B", "C", "A" }));
            Assert.That(visited, Is.Unique);
            // The defining post-order property.
            nav.AssertChildrenFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void PostOrder_Tree_YieldsEveryNodeAfterItsDescendants(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Tree(), kind);

            var visited = nav.PostOrder("A");

            Assert.That(visited, Is.EqualTo(new[] { "D", "E", "B", "F", "C", "A" }));
            nav.AssertChildrenFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void PostOrder_ComplexDag_YieldsEveryNodeAfterItsDescendants(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.PostOrder("A");

            Assert.That(visited, Is.EqualTo(new[] { "H", "F", "C", "G", "D", "A" }));
            Assert.That(visited, Is.Unique);
            nav.AssertChildrenFirst(visited);
        }

        // --- Pruning -----------------------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void OnVisit_ReturningFalse_PrunesTheNodeAndItsSubtree(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Tree(), kind);

            // B's descendants D and E go too, being reachable only through B. B itself is also not
            // yielded: the callback runs on it, but the enumerator skips it.
            var visited = nav.Downwards("A", label => label != "B");

            Assert.That(visited, Is.EqualTo(new[] { "A", "C", "F" }));
        }

        // --- Topological -------------------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ParentsFirst_Diamond_YieldsEveryNodeAfterItsParents(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.ParentsFirst();

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "C", "D" }));
            nav.AssertParentsFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ParentsFirst_TwoRoots_YieldsConvergingNodeAfterBothRoots(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.TwoRoots(), kind);

            var visited = nav.ParentsFirst();

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "C", "D" }));
            nav.AssertParentsFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ParentsFirst_ComplexDag_YieldsEveryNodeAfterItsParents(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.ParentsFirst();

            Assert.That(visited, Is.EqualTo(new[] { "A", "B", "C", "D", "E", "F", "G", "H" }));
            Assert.That(visited, Is.Unique);
            nav.AssertParentsFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ChildrenFirst_Diamond_YieldsEveryNodeAfterItsChildren(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            var visited = nav.ChildrenFirst();

            // Reverse topological: leaf first, root last.
            Assert.That(visited, Is.EqualTo(new[] { "D", "B", "C", "A" }));
            nav.AssertChildrenFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ChildrenFirst_TwoRoots_YieldsBothRootsLast(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.TwoRoots(), kind);

            var visited = nav.ChildrenFirst();

            Assert.That(visited, Is.EqualTo(new[] { "D", "C", "A", "B" }));
            nav.AssertChildrenFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ChildrenFirst_ComplexDag_YieldsEveryNodeAfterItsChildren(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            var visited = nav.ChildrenFirst();

            Assert.That(visited, Is.EqualTo(new[] { "H", "F", "G", "C", "D", "E", "A", "B" }));
            Assert.That(visited, Is.Unique);
            nav.AssertChildrenFirst(visited);
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void ChildrenFirst_Chain_IsTheReverseOfParentsFirst(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Chain("A", "B", "C"), kind);

            Assert.That(nav.ParentsFirst(), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(nav.ChildrenFirst(), Is.EqualTo(new[] { "C", "B", "A" }));
        }

        // --- Roots and leaves --------------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Roots_And_Leaves_Diamond(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            Assert.That(nav.Roots(), Is.EqualTo(new[] { "A" }));
            Assert.That(nav.Leaves(), Is.EqualTo(new[] { "D" }));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Roots_And_Leaves_TwoRoots(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.TwoRoots(), kind);

            Assert.That(nav.Roots(), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(nav.Leaves(), Is.EqualTo(new[] { "D" }));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Roots_And_Leaves_ComplexDag(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.ComplexDag(), kind);

            Assert.That(nav.Roots(), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(nav.Leaves(), Is.EqualTo(new[] { "H" }));
        }

        // --- Degenerate graphs -------------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void SingleNode_IsBothRootAndLeaf_AndTraversesToItself(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Single(), kind);
            var only = new[] { "A" };

            Assert.That(nav.Downwards("A"), Is.EqualTo(only));
            Assert.That(nav.Upwards("A"), Is.EqualTo(only));
            Assert.That(nav.ParentsFirst(), Is.EqualTo(only));
            Assert.That(nav.ChildrenFirst(), Is.EqualTo(only));
            Assert.That(nav.Roots(), Is.EqualTo(only));
            Assert.That(nav.Leaves(), Is.EqualTo(only));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void EmptyGraph_TraversesNothing(NodeKind kind)
        {
            var nav = new NodeKindTraversal(new TestGraphBuilder(), kind);

            Assert.That(nav.ParentsFirst(), Is.Empty);
            Assert.That(nav.ChildrenFirst(), Is.Empty);
            Assert.That(nav.Roots(), Is.Empty);
            Assert.That(nav.Leaves(), Is.Empty);
        }

        // --- Shorthand equivalence ---------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void Downwards_MatchesRecursiveWithExplicitArguments(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Tree(), kind);

            var shorthand = nav.Downwards("A");
            var explicitCall = nav.Recursive("A", Direction.Downwards, TraversalMethod.DepthFirstPre);

            Assert.That(shorthand, Is.EqualTo(explicitCall));
        }

        // --- Shared visited sets -----------------------------------------------------------------

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void SharedVisitedContext_SecondTraversalSkipsAlreadyVisitedNodes(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            using (nav.SharedVisited())
            {
                Assert.That(nav.Downwards("A"), Is.EqualTo(new[] { "A", "B", "D", "C" }));
                Assert.That(nav.Downwards("A"), Is.Empty);
            }
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void SharedVisitedContext_DisposeRestoresIndependentTraversals(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            using (nav.SharedVisited())
            {
                nav.Downwards("A");
            }

            // Each traversal owns its visited set again.
            Assert.That(nav.Downwards("A"), Is.EqualTo(new[] { "A", "B", "D", "C" }));
        }

        [TestCase(NodeKind.Task)]
        [TestCase(NodeKind.Data)]
        public void SharedVisitedContext_Nested_Throws(NodeKind kind)
        {
            var nav = new NodeKindTraversal(TestGraphBuilder.Diamond(), kind);

            using (nav.SharedVisited())
            {
                Assert.Throws<InvalidOperationException>(() => nav.SharedVisited());
            }
        }

        [Test]
        public void SharedVisitedContext_OneKindOnly_LeavesTheOtherKindUnaffected()
        {
            var data = new NodeKindTraversal(TestGraphBuilder.Diamond(), NodeKind.Data);
            // Same traverser, so a leaked task visited set would actually be observable here.
            var task = data.With(NodeKind.Task);

            using (data.SharedVisited())
            {
                data.Downwards("A");
                Assert.That(task.Downwards("A"), Is.EqualTo(new[] { "A", "B", "D", "C" }));
            }
        }
    }
}
