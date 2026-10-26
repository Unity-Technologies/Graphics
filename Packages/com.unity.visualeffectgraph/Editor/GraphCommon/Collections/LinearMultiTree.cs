using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    [Serializable]
    class LinearMultiTree<T> : IMultiTree<T>
    {
        [SerializeField]
        List<T> m_Data = new();

        [SerializeField]
        List<bool> m_Alive = new();

        [SerializeField]
        LinearMultiList<int> m_Children = new();

        [SerializeField]
        List<int> m_Parents = new();

        [SerializeField]
        List<int> m_RootIndices = new();

        [SerializeField]
        RootList m_RootList = new();

        public uint Version { get; private set; }

        public int Count => m_Data.Count;

        public MultiTreeNodeEnumerable<IndirectEnumerable, T> RootNodes => new(this, new IndirectEnumerable(m_RootList, m_RootList.Count));

        public MultiTreeNode<T> this[int index] => new(this, index);

        public int AddItem(T item, int parentIndex, int childCapacity = 0)
        {
            int index = m_Data.Count;
            m_Data.Add(item);
            m_Alive.Add(true);
            m_Children.AddList(childCapacity);
            m_Parents.Add(parentIndex);
            int rootIndex = index;
            if (parentIndex >= 0)
            {
                m_Children.AddItem(parentIndex, index);
                rootIndex = m_RootIndices[parentIndex];
            }
            else
            {
                m_RootList.Roots.Add(index);
            }
            m_RootIndices.Add(rootIndex);
            Version++;
            return index;
        }

        public T GetData(int index) => m_Data[index];
        public void SetData(int index, T data) => m_Data[index] = data;

        public bool IsAlive(int index) => m_Alive[index];

        /// <summary>
        /// Removes the single node at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the node to remove.</param>
        public void RemoveNode(int index)
        {
            if (index < 0 || index >= m_Data.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int parent = m_Parents[index];
            if (parent >= 0)
            {
                if (m_Children.FindItem(parent, index, out int itemIndex))
                    m_Children.RemoveItem(parent, itemIndex);
            }
            else
            {
                m_RootList.Roots.Remove(index);
            }

            var children = m_Children[index];
            for (int i = 0; i < children.Count; ++i)
            {
                int child = children[i];
                m_Parents[child] = -1;
                PromoteToRoot(child);
            }

            m_Alive[index] = false;
            m_Data[index] = default;
            Version++;
        }

        // Registers newRoot as a root and sets m_RootIndices for its whole subtree to newRoot.
        void PromoteToRoot(int newRoot)
        {
            m_RootList.Roots.Add(newRoot);
            PropagateRootIndex(newRoot, newRoot);
        }

        void PropagateRootIndex(int index, int rootIndex)
        {
            m_RootIndices[index] = rootIndex;
            var children = m_Children[index];
            for (int i = 0; i < children.Count; ++i)
                PropagateRootIndex(children[i], rootIndex);
        }

        /// <summary>
        /// Replaces the contents of this tree with a deep copy of <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The source tree to copy from.</param>
        public void CopyFrom(LinearMultiTree<T> other)
        {
            m_Data.Clear();
            m_Data.AddRange(other.m_Data);
            m_Alive.Clear();
            m_Alive.AddRange(other.m_Alive);
            m_Children.CopyFrom(other.m_Children);
            m_Parents.Clear();
            m_Parents.AddRange(other.m_Parents);
            m_RootIndices.Clear();
            m_RootIndices.AddRange(other.m_RootIndices);
            m_RootList.Roots.Clear();
            m_RootList.Roots.AddRange(other.m_RootList.Roots);
            Version = other.Version;
        }

        public void SetRootData(int index, T data) => m_Data[m_RootList[index]] = data;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new();
            sb.AppendLine("LinearMultiTree:");

            void DrawNode(int index, string indent, bool isLast)
            {
                if (!m_Alive[index])
                    return;

                sb.Append(indent);
                sb.Append(isLast ? "└── " : "├── ");
                sb.AppendLine(m_Data[index]?.ToString());

                var children = m_Children[index];
                for (int i = 0; i < children.Count; i++)
                {
                    bool lastChild = i == children.Count - 1;
                    DrawNode(children[i], indent + (isLast ? "    " : "│   "), lastChild);
                }
            }

            var roots = m_RootList.Roots;
            for (int i = 0; i < roots.Count; i++)
            {
                bool lastRoot = i == roots.Count - 1;
                DrawNode(roots[i], $"{i}", lastRoot);
            }

            return sb.ToString();
        }

        MultiTreeNode<T> IMultiTree<T>.GetRootNode(int index) => new(this, m_RootIndices[index]);

        MultiTreeNode<T>? IMultiTree<T>.GetParentNode(int index)
        {
            if (!m_Alive[index])
                return null;
            return m_Parents[index] >= 0 ? new(this, m_Parents[index]) : null;
        }

        MultiTreeNodeChildren<T> IMultiTree<T>.GetChildren(int index) => new(this, new SubEnumerable<int>(m_Children, index, m_Children[index].Count));

        [Serializable]
        class RootList : IIndexable<int, int>, ICountable
        {
            [SerializeField]
            List<int> m_Roots = new();

            public int Count => m_Roots.Count;

            public List<int> Roots => m_Roots;

            public int this[int index]
            {
                get => m_Roots[index];
                set => m_Roots[index] = value;
            }
        }
    }
}
