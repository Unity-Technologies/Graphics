using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
        /// <summary>
        /// Adapter bridging a node kind (<see cref="DataNode"/> or <see cref="TaskNode"/>) to the
        /// generic traversal core.
        /// </summary>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TId">The node id type.</typeparam>
        /// <typeparam name="TLinks">The node links collection type.</typeparam>
        /// <typeparam name="TNodeEnumerator">The enumerator type over all live nodes of the graph.</typeparam>
        public interface ITraversalAdapter<TNode, TId, TLinks, TNodeEnumerator>
            where TId : struct, IEquatable<TId>
            where TNodeEnumerator : struct, IValueEnumerator<TNode>
        {
            /// <summary>Gets the id of the node.</summary>
            TId GetId(in TNode node);
            /// <summary>Gets the links of the node followed in the given traversal direction.</summary>
            TLinks GetLinks(in TNode node, Direction direction);
            /// <summary>Gets the number of links in the collection.</summary>
            int GetLinkCount(in TLinks links);
            /// <summary>Gets the id of the linked node at the given index.</summary>
            TId GetLinkId(in TLinks links, int index);
            /// <summary>Resolves an id to its node in the graph.</summary>
            TNode Resolve(IReadOnlyGraph graph, TId id);
            /// <summary>Returns an enumerator over all live nodes of the graph.</summary>
            TNodeEnumerator GetNodes(IReadOnlyGraph graph);
            /// <summary>
            /// Seeds Kahn's algorithm state for a topological traversal: records each live node's
            /// in-degree — with edges oriented along the traversal direction, i.e. the parent count
            /// when traversing downwards, the child count when traversing upwards — in
            /// <paramref name="inDegrees"/>, and enqueues the nodes with in-degree zero in
            /// <paramref name="ready"/>.
            /// </summary>
            void SeedInDegrees(IReadOnlyGraph graph, Direction direction, Dictionary<TId, int> inDegrees, Deque<TId> ready);
        }

        /// <summary>
        /// Pooled per-node-kind working state used by the traversal enumerators: work deques and
        /// visited/discovered id sets, plus the shared visited set while a
        /// <see cref="SharedVisitedContext"/> is active.
        /// </summary>
        /// <typeparam name="TId">The node id type.</typeparam>
        internal class TraversalPools<TId>
        {
            public readonly ObjectPool<Deque<TId>> DequePool = new(() => new Deque<TId>());
            public readonly ObjectPool<HashSet<TId>> SetPool = new(() => new HashSet<TId>());
            public readonly ObjectPool<Dictionary<TId, int>> CountsPool = new(() => new Dictionary<TId, int>());

            /// <summary>Non-null while a <see cref="SharedVisitedContext"/> is active for this node kind.</summary>
            public HashSet<TId> SharedVisited;

            public void AcquireSharedVisited()
            {
                if (SharedVisited != null)
                    throw new InvalidOperationException("Cannot have nested SharedVisitedContext");
                SharedVisited = SetPool.Get();
            }

            public void ReleaseSharedVisited()
            {
                SharedVisited.Clear();
                SetPool.Release(SharedVisited);
                SharedVisited = null;
            }
        }

        /// <summary>
        /// Represents an enumerable collection for recursive traversal of graph nodes.
        /// </summary>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TId">The node id type.</typeparam>
        /// <typeparam name="TLinks">The node links collection type.</typeparam>
        /// <typeparam name="TNodeEnumerator">The enumerator type over all live nodes of the graph.</typeparam>
        /// <typeparam name="TAdapter">The adapter selecting the node kind.</typeparam>
        public readonly struct TraversalEnumerable<TNode, TId, TLinks, TNodeEnumerator, TAdapter>
            where TNode : struct
            where TId : struct, IEquatable<TId>
            where TNodeEnumerator : struct, IValueEnumerator<TNode>
            where TAdapter : struct, ITraversalAdapter<TNode, TId, TLinks, TNodeEnumerator>
        {
            private readonly TId m_StartId;
            private readonly IReadOnlyGraph m_Graph;
            private readonly TraversalPools<TId> m_Pools;
            private readonly Direction m_Direction;
            private readonly TraversalMethod m_Method;
            private readonly OnVisitNode<TNode> m_OnVisit;

            internal TraversalEnumerable(in TNode startNode, Direction direction, TraversalMethod method,
                IReadOnlyGraph graph, TraversalPools<TId> pools, OnVisitNode<TNode> onVisit)
            {
                m_StartId = default(TAdapter).GetId(in startNode);
                m_Graph = graph;
                m_Pools = pools;
                m_Direction = direction;
                m_Method = method;
                m_OnVisit = onVisit;
            }

            /// <summary>
            /// Gets an enumerator for traversing the nodes.
            /// </summary>
            /// <returns>An enumerator that can be used to traverse the nodes.</returns>
            public Enumerator GetEnumerator() =>
                new Enumerator(m_StartId, m_Direction, m_Method, m_Graph, m_Pools, m_OnVisit);

            /// <summary>
            /// Executes the traversal by iterating through all nodes using the enumerator.
            /// </summary>
            public void Execute()
            {
                using var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }

            /// <summary>
            /// Enumerator for recursive traversal of graph nodes.
            /// </summary>
            public struct Enumerator : IDisposable
            {
                private Deque<TId> m_Deque;
                private HashSet<TId> m_Discovered;
                private HashSet<TId> m_Visited;
                private bool m_OwnsVisited;
                private IReadOnlyGraph m_Graph;
                private TraversalPools<TId> m_Pools;
                private TNode m_Current;
                private Direction m_Direction;
                private TraversalMethod m_Method;
                private OnVisitNode<TNode> m_OnVisit;

                internal Enumerator(TId startId, Direction direction, TraversalMethod method,
                    IReadOnlyGraph graph, TraversalPools<TId> pools, OnVisitNode<TNode> onVisit)
                {
                    m_Direction = direction;
                    m_Method = method;
                    m_Graph = graph;
                    m_Pools = pools;
                    m_OwnsVisited = pools.SharedVisited == null;
                    m_Visited = m_OwnsVisited ? pools.SetPool.Get() : pools.SharedVisited;
                    m_Deque = pools.DequePool.Get();
                    m_Discovered = method == TraversalMethod.DepthFirstPost ? pools.SetPool.Get() : null;
                    m_Current = default;
                    m_OnVisit = onVisit;
                    m_Deque.AddFront(startId);
                }

                /// <summary>
                /// Advances the enumerator to the next node in the traversal.
                /// </summary>
                /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
                public bool MoveNext()
                {
                    if (m_Method == TraversalMethod.DepthFirstPost)
                        return MoveNextDepthFirstPost();

                    return MoveNextPreOrder();
                }

                // Two-visit DFS post-order: each node is pushed onto the work deque twice.
                // First pop → expand children and re-push self.
                // Second pop → yield (all descendants have been yielded already).
                // Correctly handles DAGs where a descendant is reachable from multiple ancestors:
                // when a node is already discovered/visited via another path, it is skipped.
                bool MoveNextDepthFirstPost()
                {
                    var adapter = default(TAdapter);
                    while (m_Deque.Count > 0)
                    {
                        var id = m_Deque.RemoveFront();

                        if (m_Visited.Contains(id))
                            continue; // already yielded or pruned via another path in the DAG

                        if (m_Discovered.Contains(id))
                        {
                            // Second encounter: all reachable descendants have been yielded.
                            m_Visited.Add(id);
                            m_Current = adapter.Resolve(m_Graph, id);
                            return true;
                        }

                        // First encounter: expand children, then schedule self for yield.
                        m_Discovered.Add(id);
                        var node = adapter.Resolve(m_Graph, id);
                        if (m_OnVisit == null || m_OnVisit.Invoke(node))
                        {
                            m_Deque.AddFront(id); // will be yielded on the second pop
                            var links = adapter.GetLinks(in node, m_Direction);
                            for (int i = adapter.GetLinkCount(in links) - 1; i >= 0; i--)
                            {
                                var neighborId = adapter.GetLinkId(in links, i);
                                if (!m_Visited.Contains(neighborId) && !m_Discovered.Contains(neighborId))
                                    m_Deque.AddFront(neighborId);
                            }
                        }
                        else
                        {
                            // Pruned: mark visited so other DAG paths also skip this node.
                            m_Visited.Add(id);
                        }
                    }

                    m_Current = default;
                    return false;
                }

                // Handles both DepthFirstPre and BreadthFirst.
                // Both use AddFront for neighbors. The dequeue end differs:
                //   DepthFirstPre: RemoveFront (LIFO stack)
                //   BreadthFirst:  RemoveBack  (FIFO queue via AddFront + RemoveBack)
                bool MoveNextPreOrder()
                {
                    var adapter = default(TAdapter);
                    bool breadthFirst = m_Method == TraversalMethod.BreadthFirst;
                    while (m_Deque.Count > 0)
                    {
                        var id = breadthFirst ? m_Deque.RemoveBack() : m_Deque.RemoveFront();

                        if (!m_Visited.Add(id))
                            continue;

                        var node = adapter.Resolve(m_Graph, id);
                        if (m_OnVisit != null && !m_OnVisit.Invoke(node))
                            continue; // pruned: not yielded, children not expanded

                        var links = adapter.GetLinks(in node, m_Direction);
                        int count = adapter.GetLinkCount(in links);
                        if (breadthFirst)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                var neighborId = adapter.GetLinkId(in links, i);
                                if (!m_Visited.Contains(neighborId))
                                    m_Deque.AddFront(neighborId);
                            }
                        }
                        else
                        {
                            // Push in reversed order so the first child is at the front of the stack.
                            for (int i = count - 1; i >= 0; i--)
                            {
                                var neighborId = adapter.GetLinkId(in links, i);
                                if (!m_Visited.Contains(neighborId))
                                    m_Deque.AddFront(neighborId);
                            }
                        }

                        m_Current = node;
                        return true;
                    }

                    m_Current = default;
                    return false;
                }

                /// <summary>
                /// Gets the current node in the traversal.
                /// </summary>
                public TNode Current => m_Current;

                /// <summary>
                /// Releases all resources used by the enumerator.
                /// </summary>
                public void Dispose()
                {
                    if (m_OwnsVisited)
                    {
                        m_Visited.Clear();
                        m_Pools.SetPool.Release(m_Visited);
                    }

                    m_Deque.Clear();
                    m_Pools.DequePool.Release(m_Deque);

                    if (m_Discovered != null)
                    {
                        m_Discovered.Clear();
                        m_Pools.SetPool.Release(m_Discovered);
                    }
                }
            }
        }

        /// <summary>
        /// Represents an enumerable collection for linear traversal of graph nodes with optional
        /// filtering.
        /// </summary>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TId">The node id type.</typeparam>
        /// <typeparam name="TLinks">The node links collection type.</typeparam>
        /// <typeparam name="TNodeEnumerator">The enumerator type over all live nodes of the graph.</typeparam>
        /// <typeparam name="TAdapter">The adapter selecting the node kind.</typeparam>
        public readonly struct LinearEnumerable<TNode, TId, TLinks, TNodeEnumerator, TAdapter>
            where TId : struct, IEquatable<TId>
            where TNodeEnumerator : struct, IValueEnumerator<TNode>
            where TAdapter : struct, ITraversalAdapter<TNode, TId, TLinks, TNodeEnumerator>
        {
            private readonly IReadOnlyGraph m_Graph;
            private readonly OnFilterNode<TNode> m_OnFilter;

            internal LinearEnumerable(IReadOnlyGraph graph, OnFilterNode<TNode> onFilter)
            {
                m_Graph = graph;
                m_OnFilter = onFilter;
            }

            /// <summary>
            /// Gets an enumerator for traversing the nodes linearly.
            /// </summary>
            /// <returns>An enumerator for the nodes.</returns>
            public Enumerator GetEnumerator() => new Enumerator(m_Graph, m_OnFilter);

            /// <summary>
            /// Executes the traversal, iterating through all nodes.
            /// </summary>
            public void Execute()
            {
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }

            /// <summary>
            /// Enumerator for linear traversal of graph nodes with filtering.
            /// </summary>
            public struct Enumerator
            {
                private TNodeEnumerator m_Inner;
                private OnFilterNode<TNode> m_OnFilter;
                private bool m_Stopped;

                internal Enumerator(IReadOnlyGraph graph, OnFilterNode<TNode> onFilter)
                {
                    m_Inner = default(TAdapter).GetNodes(graph);
                    m_OnFilter = onFilter;
                    m_Stopped = false;
                }

                /// <summary>
                /// Gets the current node in the traversal.
                /// </summary>
                public TNode Current => m_Inner.Current;

                /// <summary>
                /// Advances the enumerator to the next node that matches the filter.
                /// </summary>
                /// <returns>True if a node was found; otherwise, false.</returns>
                public bool MoveNext()
                {
                    if (m_Stopped) return false;
                    if (m_OnFilter == null) return m_Inner.MoveNext();
                    while (m_Inner.MoveNext())
                    {
                        var control = m_OnFilter(m_Inner.Current);
                        if (!control.Continue) m_Stopped = true;
                        if (control.Accept) return true;
                        if (m_Stopped) return false;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Represents an enumerable collection that yields graph nodes in topological order:
        /// parent-first when traversing <see cref="Direction.Downwards"/>, children-first (reverse
        /// topological) when traversing <see cref="Direction.Upwards"/>.
        /// </summary>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TId">The node id type.</typeparam>
        /// <typeparam name="TLinks">The node links collection type.</typeparam>
        /// <typeparam name="TNodeEnumerator">The enumerator type over all live nodes of the graph.</typeparam>
        /// <typeparam name="TAdapter">The adapter selecting the node kind.</typeparam>
        public readonly struct TopologicalEnumerable<TNode, TId, TLinks, TNodeEnumerator, TAdapter>
            where TNode : struct
            where TId : struct, IEquatable<TId>
            where TNodeEnumerator : struct, IValueEnumerator<TNode>
            where TAdapter : struct, ITraversalAdapter<TNode, TId, TLinks, TNodeEnumerator>
        {
            private readonly IReadOnlyGraph m_Graph;
            private readonly TraversalPools<TId> m_Pools;
            private readonly Direction m_Direction;

            internal TopologicalEnumerable(IReadOnlyGraph graph, TraversalPools<TId> pools, Direction direction)
            {
                m_Graph = graph;
                m_Pools = pools;
                m_Direction = direction;
            }

            /// <summary>
            /// Gets the enumerator for traversing the nodes in topological order.
            /// </summary>
            /// <returns>An enumerator that yields the nodes in topological order.</returns>
            public Enumerator GetEnumerator() => new Enumerator(m_Graph, m_Pools, m_Direction);

            /// <summary>
            /// Executes the traversal, iterating through all nodes.
            /// </summary>
            public void Execute()
            {
                using var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }

            /// <summary>
            /// Enumerator that yields graph nodes in topological order using Kahn's algorithm.
            /// </summary>
            public struct Enumerator : IDisposable
            {
                private IReadOnlyGraph m_Graph;
                private TraversalPools<TId> m_Pools;
                private Deque<TId> m_Ready;
                // Remaining in-degree per node; a node becomes ready at zero. See SeedInDegrees.
                private Dictionary<TId, int> m_InDegrees;
                private Direction m_Direction;
                private TNode m_Current;

                internal Enumerator(IReadOnlyGraph graph, TraversalPools<TId> pools, Direction direction)
                {
                    m_Graph = graph;
                    m_Pools = pools;
                    m_Ready = pools.DequePool.Get();
                    m_InDegrees = pools.CountsPool.Get();
                    m_Direction = direction;
                    m_Current = default;

                    default(TAdapter).SeedInDegrees(graph, direction, m_InDegrees, m_Ready);
                }

                /// <summary>
                /// Advances the enumerator to the next node in topological order.
                /// </summary>
                /// <returns>true if the enumerator was advanced to the next element; false if it passed the end of the collection.</returns>
                public bool MoveNext()
                {
                    if (m_Ready.Count == 0 && m_InDegrees.Count > 0)
                    {
                        // No node with in-degree zero remains, yet some nodes are unvisited: this
                        // implies a cycle in a graph we expect to be acyclic. Emit the rest defensively.
                        UnityEngine.Debug.Assert(false, "Cycle detected during topological traversal.");
                        foreach (var entry in m_InDegrees)
                            m_Ready.AddBack(entry.Key);
                        m_InDegrees.Clear();
                    }

                    if (m_Ready.Count == 0)
                    {
                        m_Current = default;
                        return false;
                    }

                    var adapter = default(TAdapter);
                    var id = m_Ready.RemoveFront();
                    m_Current = adapter.Resolve(m_Graph, id);
                    m_InDegrees.Remove(id);

                    // Yielding this node decrements the in-degree of the nodes it links to in the
                    // traversal direction: children when parent-first, parents when children-first.
                    var links = adapter.GetLinks(in m_Current, m_Direction);
                    int count = adapter.GetLinkCount(in links);
                    for (int i = 0; i < count; i++)
                    {
                        var linkedId = adapter.GetLinkId(in links, i);
                        if (m_InDegrees.TryGetValue(linkedId, out int remaining))
                        {
                            remaining--;
                            m_InDegrees[linkedId] = remaining;
                            if (remaining == 0)
                                m_Ready.AddBack(linkedId);
                        }
                    }
                    return true;
                }

                /// <summary>
                /// Gets the current node in the traversal.
                /// </summary>
                public TNode Current => m_Current;

                /// <summary>
                /// Releases all resources used by the enumerator.
                /// </summary>
                public void Dispose()
                {
                    m_Ready.Clear();
                    m_Pools.DequePool.Release(m_Ready);
                    m_InDegrees.Clear();
                    m_Pools.CountsPool.Release(m_InDegrees);
                }
            }
        }
    }
}
