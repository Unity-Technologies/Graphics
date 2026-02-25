using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphCommon.LowLevel.Editor
{
    [Serializable]
    class LinearGraph<T> : IGraph<T>
    {
        [SerializeField]
        List<T> m_Data = new();

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
        public int AddItem(T item, int parentCapacity = 0, int childCapacity = 0)        {
            int index = m_Data.Count;
            m_Data.Add(item);
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

        /// <summary>Gets the capacity of the parents list for the specified node.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The capacity of the parents list.</returns>
        public int GetParentsCapacity(int index) => m_Parents[index].Capacity;

        /// <summary>Gets the capacity of the children list for the specified node.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The capacity of the children list.</returns>
        public int GetChildrenCapacity(int index) => m_Children[index].Capacity;

        /// <summary>Returns an enumerator that iterates through the graph nodes.</summary>
        /// <returns>An enumerator for the graph.</returns>
        public LinearEnumerator<LinearGraph<T>, GraphNode<T>> GetEnumerator() => new(this);
        T IGraph<T>.GetData(int index) => m_Data[index];
        GraphNodeLinks<T> IGraph<T>.GetParents(int index) => new GraphNodeLinks<T>(this, m_Parents, index, m_Parents[index].Count);
        GraphNodeLinks<T> IGraph<T>.GetChildren(int index) => new GraphNodeLinks<T>(this, m_Children, index, m_Children[index].Count);
    }
}
