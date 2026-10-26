using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphCommon.LowLevel.Editor
{
    [Serializable]
    class LinearDirectedGraph<T> : IDirectedGraph<T>
    {
        [SerializeField]
        List<T> m_Data = new();

        [SerializeField]
        List<bool> m_Alive = new();

        [SerializeField]
        LinearMultiList<int> m_Parents = new();

        [SerializeField]
        LinearMultiList<int> m_Children = new();

        /// <summary>Gets the number of nodes in the graph.</summary>
        public int Count => m_Data.Count;

        /// <summary>Gets the current version of the graph, incremented when the graph structure changes.</summary>
        public uint Version { get; private set; } = 1;

        /// <summary>Gets the graph node at the specified index.</summary>
        /// <param name="index">The zero-based index of the node to get.</param>
        /// <returns>The graph node at the specified index.</returns>
        public GraphNode<T> this[int index] => new(this, index);

        /// <summary>
        /// Adds a new item to the graph.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="parentCapacity">Initial capacity for parent connections. Default is 0.</param>
        /// <param name="childCapacity">Initial capacity for child connections. Default is 0.</param>
        /// <returns>The index of the newly added item.</returns>
        public int AddItem(T item, int parentCapacity = 0, int childCapacity = 0)
        {
            int index = m_Data.Count;
            m_Data.Add(item);
            m_Alive.Add(true);
            m_Parents.AddList(parentCapacity);
            m_Children.AddList(childCapacity);
            return index;
        }

        /// <summary>
        /// Creates a connection between two nodes, establishing a parent-child relationship.
        /// </summary>
        /// <param name="parentIndex">The index of the parent node.</param>
        /// <param name="childIndex">The index of the child node.</param>
        public void Connect(int parentIndex, int childIndex)
        {
            Assert.IsFalse(m_Parents.FindItem(childIndex, parentIndex, out _));
            m_Parents.AddItem(childIndex, parentIndex);

            Assert.IsFalse(m_Children.FindItem(parentIndex, childIndex, out _));
            m_Children.AddItem(parentIndex, childIndex);
        }

        /// <summary>
        /// Removes a connection between two nodes, breaking the parent-child relationship.
        /// </summary>
        /// <param name="parentIndex">The index of the parent node.</param>
        /// <param name="childIndex">The index of the child node.</param>
        public void Disconnect(int parentIndex, int childIndex)
        {
            int itemIndex = 0;

            if (m_Parents.FindItem(childIndex, parentIndex, out itemIndex))
                m_Parents.RemoveItem(childIndex, itemIndex);

            if (m_Children.FindItem(parentIndex, childIndex, out itemIndex))
                m_Children.RemoveItem(parentIndex, itemIndex);
        }

        /// <summary>
        /// Removes the node at <paramref name="index"/>: marks the slot dead and disconnects it from
        /// every parent and child so no dangling edges point at (or out of) the removed node.
        /// </summary>
        /// <param name="index">The slot index of the node to remove.</param>
        /// <remarks>
        /// The slot itself is retained (so indices of other nodes stay stable) but is reported as not
        /// alive by <see cref="IsAlive"/> and skipped by enumerators.
        /// </remarks>
        public void RemoveNode(int index)
        {
            if (index < 0 || index >= m_Data.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var parents = m_Parents[index];
            foreach (var parent in parents)
            {
                if (m_Children.FindItem(parent, index, out int itemIndex))
                    m_Children.RemoveItem(parent, itemIndex);
            }

            var children = m_Children[index];
            foreach (var child in children)
            {
                if (m_Parents.FindItem(child, index, out int itemIndex))
                    m_Parents.RemoveItem(child, itemIndex);
            }

            m_Parents.ClearList(index);
            m_Children.ClearList(index);

            m_Alive[index] = false;
            m_Data[index] = default;
            Version++;
        }

        /// <inheritdoc cref="IDirectedGraph{T}.IsAlive"/>
        public bool IsAlive(int index) => m_Alive[index];

        /// <summary>Gets the capacity of the parents list for the specified node.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The capacity of the parents list.</returns>
        public int GetParentsCapacity(int index) => m_Parents[index].Capacity;

        /// <summary>Gets the capacity of the children list for the specified node.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The capacity of the children list.</returns>
        public int GetChildrenCapacity(int index) => m_Children[index].Capacity;

        /// <summary>Overwrites the data stored at <paramref name="index"/> without touching edges.</summary>
        public void SetData(int index, T data) => m_Data[index] = data;

        /// <summary>
        /// Replaces the contents of this graph with a deep copy of <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The source graph to copy from.</param>
        public void CopyFrom(LinearDirectedGraph<T> other)
        {
            m_Data.Clear();
            m_Data.AddRange(other.m_Data);
            m_Alive.Clear();
            m_Alive.AddRange(other.m_Alive);
            m_Parents.CopyFrom(other.m_Parents);
            m_Children.CopyFrom(other.m_Children);
            Version = other.Version;
        }

        T IDirectedGraph<T>.GetData(int index) => m_Data[index];
        GraphNodeLinks<T> IDirectedGraph<T>.GetParents(int index) => new(this, m_Parents, index, m_Parents[index].Count);
        GraphNodeLinks<T> IDirectedGraph<T>.GetChildren(int index) => new(this, m_Children, index, m_Children[index].Count);
    }
}
