using System;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class RemoveRangeExtensionsTests
    {
        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData(new int[] {1,2,3,4,5,6}, 1, 2).SetName("Remove middle"),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 0, 2).SetName("Remove front"),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 0, 6).SetName("Remove all"),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, 1).SetName("Remove back"),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 0, 0).SetName("Index 0"),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, 0).SetName("Count 0")
        };

        bool ItemInRangeAreRemovedAfterRemoveRange<TList>(TList list, int startIndex, int count)
            where TList : IList<int>
        {
            using (ListPool<int>.Get(out var copy))
            {
                foreach (int integer in list)
                    copy.Add(integer);

                if (list.TryRemoveElementsInRange(startIndex, count, out var exception))
                {
                    copy.RemoveRange(startIndex, count);
                    return copy.SequenceEqual(list);
                }

                return false;
            }
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public void ItemInRangeAreRemovedAfterRemoveRangeForList(int[] ints, int startIndex, int count)
        {
            using (GenericPool<SimpleList>.Get(out var copy))
            {
                copy.AddRange(ints);
                Assert.IsTrue(ItemInRangeAreRemovedAfterRemoveRange<IList<int>>(copy as IList<int>, startIndex, count));
            }
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public void ItemInRangeAreRemovedAfterRemoveRangeForSimpleList(int[] ints, int startIndex, int count)
        {
            using (ListPool<int>.Get(out var copy))
            {
                copy.AddRange(ints);
                Assert.IsTrue(ItemInRangeAreRemovedAfterRemoveRange<List<int>>(copy, startIndex, count));
            }
        }

        static TestCaseData[] s_ListTestsCaseDatasExceptions =
        {
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, -1).SetName("Count negative").Returns(typeof(ArgumentOutOfRangeException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, -1, 2).SetName("Index negative").Returns(typeof(ArgumentOutOfRangeException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, 5).SetName("Count exceeds list size").Returns(typeof(ArgumentException)),
        };

        Exception ExceptionsAreCorrect<TList>(TList list, int startIndex, int count)
            where TList : IList<int>
        {
            list.TryRemoveElementsInRange(startIndex, count, out var error);
            return error;
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatasExceptions))]
        public Type ExceptionsAreCorrectForList(int[] ints, int startIndex, int count)
        {
            using (ListPool<int>.Get(out var copy))
            {
                copy.AddRange(ints);
                return ExceptionsAreCorrect(copy, startIndex, count).GetType();
            }
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatasExceptions))]
        public Type ExceptionsAreCorrectForSimpleList(int[] ints, int startIndex, int count)
        {
            using (GenericPool<SimpleList>.Get(out var copy))
            {
                copy.AddRange(ints);
                return ExceptionsAreCorrect(copy, startIndex, count).GetType();
            }
        }

        class SimpleList : IList<int>
        {
            private List<int> m_List = new List<int>();

            public void AddRange(int[] ints)
            {
                m_List.Clear();
                m_List.AddRange(ints);
            }

            public IEnumerator<int> GetEnumerator()
            {
                return m_List.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(int item)
            {
                m_List.Add(item);
            }

            public void Clear()
            {
                m_List.Clear();
            }

            public bool Contains(int item)
            {
                return m_List.Contains(item);
            }

            public void CopyTo(int[] array, int arrayIndex)
            {
                m_List.CopyTo(array, arrayIndex);
            }

            public bool Remove(int item)
            {
                return m_List.Remove(item);
            }

            public int Count => m_List.Count;
            public bool IsReadOnly => false;
            public int IndexOf(int item)
            {
                return m_List.IndexOf(item);
            }

            public void Insert(int index, int item)
            {
                m_List.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                m_List.RemoveAt(index);
            }

            public int this[int index]
            {
                get => m_List[index];
                set => m_List[index] = value;
            }
        }

    }
}
