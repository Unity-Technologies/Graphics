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
        LinearMultiList<int> m_Children = new();

        [SerializeField]
        List<int> m_Parents = new();

        [SerializeField]
        List<int> m_RootIndices = new();

        [SerializeField]
        RootList m_RootList = new();

        public uint Version { get; private set; }

        public int Count => m_Data.Count;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new();
            sb.AppendLine("LinearMultiTree:");

            void DrawNode(int index, string indent, bool isLast)
            {
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
        public MultiTreeNodeEnumerable<IndirectEnumerable, T> RootNodes => new(this, new IndirectEnumerable(m_RootList, m_RootList.Count));

        public MultiTreeNode<T> this[int index] => new(this, index);

        public int AddItem(T item, int parentIndex, int childCapacity = 0)
        {
            int index = m_Data.Count;
            m_Data.Add(item);
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

        MultiTreeNode<T> IMultiTree<T>.GetRootNode(int index) => new(this, m_RootIndices[index]);

        MultiTreeNode<T>? IMultiTree<T>.GetParentNode(int index) => m_Parents[index] >= 0 ? new (this, m_Parents[index]) : null;

        MultiTreeNodeEnumerable <SubEnumerable<int>, T> IMultiTree<T>.GetChildren(int index) => new(this, new SubEnumerable<int>(m_Children, index, m_Children[index].Count));

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
