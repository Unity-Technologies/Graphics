using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.Utils.Tests
{
    [TestFixture]
    class ObservableListTests
    {
        public static IEnumerable<TestCaseData> TestCasesAdd
        {
            get
            {
                yield return new TestCaseData((Action<ObservableList<int>>)(list => list.Add(5)), (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {5})
                    .SetName("AddOneElement_Sort");
                yield return new TestCaseData((Action<ObservableList<int>>)(list => list.Add(5)), null, new [] {5})
                    .SetName("AddOneElement");

                yield return new TestCaseData((Action<ObservableList<int>>)(list =>
                    {
                        list.Add(3);
                        list.Add(1);
                        list.Add(4);
                    }),
                    (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {1, 3, 4})
                    .SetName("AddMultipleELements_Sort");
                yield return new TestCaseData((Action<ObservableList<int>>)(list =>
                        {
                            list.Add(3);
                            list.Add(1);
                            list.Add(4);
                        }), null, new [] {3, 1, 4})
                    .SetName("AddMultipleELements");

                yield return new TestCaseData((Action<ObservableList<int>>)(
                        list => list.Add(3, 1, 4)), (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {1, 3, 4})
                    .SetName("AddMultipleElementsAtOnce_Sort");
                yield return new TestCaseData((Action<ObservableList<int>>)(
                        list => list.Add(3, 1, 4)),null, new [] {3, 1, 4})
                    .SetName("AddMultipleElementsAtOnce");
            }
        }

        [Test, TestCaseSource(nameof(TestCasesAdd))]
        public void Add_On_List(Action<ObservableList<int>> modify, Comparison<int> comparison,  int[] expected)
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0, comparison: comparison);
            bool itemAddedEventTriggered = false;
            list.ItemAdded += (sender, args) => itemAddedEventTriggered = true;

            modify(list);

            // Assert
            Assert.IsTrue(itemAddedEventTriggered);
            Assert.AreEqual(expected.Length, list.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        public static IEnumerable<TestCaseData> TestCasesRemove
        {
            get
            {
                yield return new TestCaseData(new int[] {5, 1}, (Action<ObservableList<int>>)(list => list.Remove(1)), (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {5})
                    .SetName("RemoveOneElement_Sort");
                yield return new TestCaseData(new int[] {5, 1}, (Action<ObservableList<int>>)(list => list.Remove(5)), null, new [] {1})
                    .SetName("RemoveOneElement");

                yield return new TestCaseData(new int[] {8,6,7,3,1,4},(Action<ObservableList<int>>)(list =>
                    {
                        list.Remove(3);
                        list.Remove(1);
                        list.Remove(4);
                    }),
                    (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {6, 7, 8})
                    .SetName("RemoveMultipleElements_Sort");
                yield return new TestCaseData(new int[] {8,6,7,3,1,4},(Action<ObservableList<int>>)(list =>
                        {
                            list.Remove(3);
                            list.Remove(1);
                            list.Remove(4);
                        }),
                        null, new [] {8, 6, 7})
                    .SetName("RemoveMultipleElements");

                yield return new TestCaseData(new int[] {5, 1}, (Action<ObservableList<int>>)(list => list.RemoveAt(0)), (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {5})
                    .SetName("RemoveAtOneElement_Sort");
                yield return new TestCaseData(new int[] {5, 1}, (Action<ObservableList<int>>)(list => list.RemoveAt(0)), null, new [] {1})
                    .SetName("RemoveAtOneElement");

                yield return new TestCaseData(new int[] {8,6,7,3,1,4},(Action<ObservableList<int>>)(list =>
                        {
                            list.RemoveAt(0);
                            list.RemoveAt(1);
                            list.RemoveAt(2);
                        }),
                        (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {3, 6, 8})
                    .SetName("RemoveAtMultipleElements_Sort");
                yield return new TestCaseData(new int[] {8,6,7,3,1,4},(Action<ObservableList<int>>)(list =>
                        {
                            list.RemoveAt(0);
                            list.RemoveAt(1);
                            list.RemoveAt(2);
                        }),
                        null, new [] {6, 3, 4})
                    .SetName("RemoveAtMultipleElements");
            }
        }

        [Test, TestCaseSource(nameof(TestCasesRemove))]
        public void Remove_From_List(int[] init, Action<ObservableList<int>> modify, Comparison<int> comparison,  int[] expected)
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(init,  comparison: comparison);
            bool itemRemovedTriggered = false;
            list.ItemRemoved += (sender, args) => itemRemovedTriggered = true;

            modify(list);

            // Assert
            Assert.IsTrue(itemRemovedTriggered);
            Assert.AreEqual(expected.Length, list.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        public static IEnumerable<TestCaseData> TestCasesInsert
        {
            get
            {
                yield return new TestCaseData(new int[] {}, (Action<ObservableList<int>>)(list => list.Insert(0, 1)), (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {1})
                    .SetName("InsertOneElement_Sort");
                yield return new TestCaseData(new int[] {}, (Action<ObservableList<int>>)(list => list.Insert(0, 1)), null, new [] {1})
                    .SetName("InsertOneElement");

                yield return new TestCaseData(new int[] {8,6,7},(Action<ObservableList<int>>)(list =>
                    {
                        list.Insert(0, 3);
                        list.Insert(0, 10);
                        list.Insert(0, 2);
                    }),
                    (Comparison<int>)((x, y) => x.CompareTo(y)), new [] {2, 3, 6, 7, 8, 10})
                    .SetName("InsertultipleElements_Sort");
                yield return new TestCaseData(new int[] {8,6,7},(Action<ObservableList<int>>)(list =>
                        {
                            list.Insert(0, 0);
                            list.Insert(0, 10);
                        }),
                        null, new [] {10, 0, 8, 6, 7})
                    .SetName("InsertMultipleElements");
            }
        }

        [Test, TestCaseSource(nameof(TestCasesInsert))]
        public void Insert_Into_List(int[] init, Action<ObservableList<int>> modify, Comparison<int> comparison,  int[] expected)
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(init,  comparison: comparison);
            bool itemRemovedTriggered = false;
            list.ItemAdded += (sender, args) => itemRemovedTriggered = true;

            modify(list);

            // Assert
            Assert.IsTrue(itemRemovedTriggered);
            Assert.AreEqual(expected.Length, list.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }

        // Test for clearing the list
        [Test]
        public void Clear_ClearsList_ItemRemovedEventTriggeredForEach_NoComparison()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0, comparison: (x, y) => x.CompareTo(y));
            list.Add(5);
            list.Add(10);
            bool itemRemovedEventTriggered = false;
            list.ItemRemoved += (sender, args) => itemRemovedEventTriggered = true;

            // Act
            list.Clear();

            // Assert
            Assert.AreEqual(0, list.Count);
            Assert.IsTrue(itemRemovedEventTriggered);
        }

        // Test for indexer getting and setting an item
        [Test]
        public void SetIndexer_SetsItem_TriggersEvents()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0,comparison: (x, y) => x.CompareTo(y));
            list.Add(1);
            list.Add(2);
            bool itemAddedEventTriggered = false;
            bool itemRemovedEventTriggered = false;
            list.ItemAdded += (sender, args) => itemAddedEventTriggered = true;
            list.ItemRemoved += (sender, args) => itemRemovedEventTriggered = true;

            // Act
            list[0] = 5;

            // Assert
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(5, list[0]);
            Assert.IsTrue(itemAddedEventTriggered);
            Assert.IsTrue(itemRemovedEventTriggered);
        }

        // Test for Contains method
        [Test]
        public void Contains_ReturnsTrueIfItemExists()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0,comparison: (x, y) => x.CompareTo(y));
            list.Add(5);

            // Act
            bool result = list.Contains(5);

            // Assert
            Assert.IsTrue(result);
        }

        // Test for IndexOf method
        [Test]
        public void IndexOf_ReturnsCorrectIndex()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0, comparison: (x, y) => x.CompareTo(y));
            list.Add(5);
            list.Add(10);

            // Act
            int index = list.IndexOf(10);

            // Assert
            Assert.AreEqual(1, index);
        }

        // Test for constructor with collection and custom comparison
        [Test]
        public void ConstructorWithCollection_SortsItems_WhenComparisonProvided()
        {
            // Arrange
            var collection = new List<int> { 3, 1, 2 };
            var list = new UnityEngine.Rendering.ObservableList<int>(collection, comparison: (x, y) => x.CompareTo(y));

            // Assert
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        // Test for constructor with custom comparison and sorting
        [Test]
        public void ConstructorWithComparison_SortsItems_WhenComparisonProvided()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>(0,comparison: (x, y) => x.CompareTo(y));

            // Act
            list.Add(10);
            list.Add(5);
            list.Add(7);

            // Assert
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(5, list[0]);
            Assert.AreEqual(7, list[1]);
            Assert.AreEqual(10, list[2]);
        }

        // Test for inserting an item at a specific index without comparison
        [Test]
        public void InsertsItemAtIndex_ItemAddedEventTriggered_NoComparison()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(1); // Ensure there is at least one item
            bool itemAddedEventTriggered = false;
            list.ItemAdded += (sender, args) => itemAddedEventTriggered = true;

            // Act
            list.Insert(1, 5); // Insert 5 at the end

            // Assert
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(itemAddedEventTriggered);
            Assert.AreEqual(5, list[1]); // Verify that 5 is inserted at index 1
        }

        // Test for inserting an item at the beginning
        [Test]
        public void InsertsItemAtBeginning_ItemAddedEventTriggered()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(10); // Ensure there is at least one item
            bool itemAddedEventTriggered = false;
            list.ItemAdded += (sender, args) => itemAddedEventTriggered = true;

            // Act
            list.Insert(0, 5); // Insert 5 at the beginning

            // Assert
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(itemAddedEventTriggered);
            Assert.AreEqual(5, list[0]); // Verify that 5 is inserted at index 0
        }

        // Test for inserting an item at an index greater than the last item index
        [Test]
        public void InsertsItemAtOutOfRangeIndex_ItemAddedEventTriggered()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(10, 5)); // Inserting at an index greater than the list size
        }

        // Test for removing an item that doesn't exist
        [Test]
        public void Remove_ItemNotInList_ReturnsFalse()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(10);
            list.Add(20);

            // Act
            bool result = list.Remove(30); // 30 is not in the list

            // Assert
            Assert.AreEqual(2, list.Count); // List should remain unchanged
            Assert.IsFalse(result); // Removal should fail
        }

        // Test for removing an item that doesn't exist using RemoveAt
        [Test]
        public void RemoveAt_IndexOutOfRange_ThrowsException()
        {
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(10);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(10)); // Trying to remove from an invalid index
        }

        // Test for inserting multiple items (with no comparison)
        [Test]
        public void InsertMultiple_AddsItemsAtSpecificIndex_ItemAddedEventTriggeredForEach()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(10);
            list.Add(20);
            bool itemAddedEventTriggered = false;
            list.ItemAdded += (sender, args) => itemAddedEventTriggered = true;

            // Act
            list.Insert(1, 15); // Insert 15 at index 1

            // Assert
            Assert.AreEqual(3, list.Count);
            Assert.IsTrue(itemAddedEventTriggered);
            Assert.AreEqual(15, list[1]); // 15 should be inserted at index 1
        }

        // Test for clearing the list
        [Test]
        public void Clear_ClearsList_ItemRemovedEventTriggeredForEach()
        {
            // Arrange
            var list = new UnityEngine.Rendering.ObservableList<int>();
            list.Add(10);
            list.Add(20);
            bool itemRemovedEventTriggered = false;
            list.ItemRemoved += (sender, args) => itemRemovedEventTriggered = true;

            // Act
            list.Clear();

            // Assert
            Assert.AreEqual(0, list.Count);
            Assert.IsTrue(itemRemovedEventTriggered); // Events should trigger for every item removed
        }
    }
}
