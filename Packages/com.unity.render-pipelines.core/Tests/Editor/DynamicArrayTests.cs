using System;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Tests
{
    class DynamicArrayTests
    {
        DynamicArray<int> m_DynamicArray;

        [SetUp]
        public void Setup()
        {
            m_DynamicArray = new DynamicArray<int>();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            m_DynamicArray = null;
        }

        [Test]
        public void TestContainElement()
        {
            m_DynamicArray.Add(2);

            Assert.IsTrue(m_DynamicArray.Contains(2));
            Assert.IsFalse(m_DynamicArray.Contains(55));
        }

        [Test]
        public void TestAddElementCorrectSize()
        {
            Assert.AreEqual(0, m_DynamicArray.size);
            m_DynamicArray.Add(2);
            Assert.AreEqual(1, m_DynamicArray.size);
            m_DynamicArray.Add(4);
            Assert.AreEqual(2, m_DynamicArray.size);
        }

        [Test]
        public void TestAddRangeCorrectElements()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            var otherArray = new DynamicArray<int>();
            otherArray.Add(3);
            otherArray.Add(4);

            m_DynamicArray.AddRange(otherArray);

            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(2, m_DynamicArray[1]);
            Assert.AreEqual(3, m_DynamicArray[2]);
            Assert.AreEqual(4, m_DynamicArray[3]);
            Assert.AreEqual(4, m_DynamicArray.size);
        }

        [Test]
        public void TestAddRangeOutOfSpaceKeepPreviousElements()
        {
            var smallDynamicArray = new DynamicArray<int>(2);
            smallDynamicArray[0] = 1;
            smallDynamicArray[1] = 2;

            var otherArray = new DynamicArray<int>();
            otherArray.Add(3);
            otherArray.Add(4);

            smallDynamicArray.AddRange(otherArray);

            Assert.AreEqual(1, smallDynamicArray[0]);
            Assert.AreEqual(2, smallDynamicArray[1]);
            Assert.AreEqual(3, smallDynamicArray[2]);
            Assert.AreEqual(4, smallDynamicArray[3]);
            Assert.AreEqual(4, smallDynamicArray.size);
        }

        [Test]
        public void TestRemoveElementCorrectSize()
        {
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(4);

            Assert.AreEqual(2, m_DynamicArray.size);

            m_DynamicArray.Remove(2);

            Assert.AreEqual(1, m_DynamicArray.size);

            m_DynamicArray.Remove(4);

            Assert.AreEqual(0, m_DynamicArray.size);
        }

        [Test]
        public void TestRemoveAtKeepOrder()
        {
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(8);

            Assert.AreEqual(3, m_DynamicArray.size);

            m_DynamicArray.RemoveAt(1);

            Assert.AreEqual(2, m_DynamicArray.size);
            Assert.AreEqual(2, m_DynamicArray[0]);
            Assert.AreEqual(8, m_DynamicArray[1]);
        }

        [Test]
        public void TestRemoveInexistantElementReturnFalse()
        {
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(4);

            Assert.IsFalse(m_DynamicArray.Remove(8));
            Assert.AreEqual(2, m_DynamicArray.size);
        }

        [Test]
        public void TestRemoveKeepOrder()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);

            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(2, m_DynamicArray[1]);
            Assert.AreEqual(3, m_DynamicArray[2]);
            Assert.AreEqual(4, m_DynamicArray[3]);
            Assert.AreEqual(4, m_DynamicArray.size);

            m_DynamicArray.Remove(2);
            Assert.IsFalse(m_DynamicArray.Contains(2));
            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(3, m_DynamicArray[1]);
            Assert.AreEqual(4, m_DynamicArray[2]);
            Assert.AreEqual(3, m_DynamicArray.size);
        }

        [Test]
        public void TestRemoveRangeKeepOrder()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(5);

            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(2, m_DynamicArray[1]);
            Assert.AreEqual(3, m_DynamicArray[2]);
            Assert.AreEqual(4, m_DynamicArray[3]);
            Assert.AreEqual(5, m_DynamicArray[4]);
            Assert.AreEqual(5, m_DynamicArray.size);

            m_DynamicArray.RemoveRange(1, 2);
            Assert.IsFalse(m_DynamicArray.Contains(2));
            Assert.IsFalse(m_DynamicArray.Contains(3));
            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(4, m_DynamicArray[1]);
            Assert.AreEqual(5, m_DynamicArray[2]);
            Assert.AreEqual(3, m_DynamicArray.size);
        }

        [Test]
        public void TestInvalidAccessThrows()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            int dummy;
            Assert.Throws<System.IndexOutOfRangeException>(() => dummy = m_DynamicArray[2]);
            Assert.Throws<System.IndexOutOfRangeException>(() => dummy = m_DynamicArray[-1]);
        }

        [Test]
        public void TestRemoveAtInvalidAccessThrows()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            Assert.Throws<System.IndexOutOfRangeException>(() => m_DynamicArray.RemoveAt(-1));
            Assert.Throws<System.IndexOutOfRangeException>(() => m_DynamicArray.RemoveAt(2));
        }

        [Test]
        public void TestRemoveRangeInvalidAccessThrows()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => m_DynamicArray.RemoveRange(1, 2));
        }

        [Test]
        public void TestRemoveRangeValidAccessDoesNotThrows()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            Assert.DoesNotThrow(() => m_DynamicArray.RemoveRange(1, 1));
        }

        [Test]
        public void TestFindIndexFailReturnMinusOne()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);

            Assert.AreEqual(-1, m_DynamicArray.FindIndex(0, m_DynamicArray.size, (x) => x == 4));

            Assert.AreEqual(-1, m_DynamicArray.FindIndex((x) => x == 4));
        }

        [Test]
        public void TestFindIndexSuccessReturnRightFirstIndex()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(2);

            Assert.AreEqual(1, m_DynamicArray.FindIndex(0, m_DynamicArray.size, (x) => x == 2));
            Assert.AreEqual(3, m_DynamicArray.FindIndex(2, 2, (x) => x == 2));

            Assert.AreEqual(1, m_DynamicArray.FindIndex((x) => x == 2));
        }

        [Test]
        public void TestFindIndexDoesNotFindOutsideCount()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);

            Assert.AreEqual(-1, m_DynamicArray.FindIndex(1, 2, (x) => x == 1));
            Assert.AreEqual(1, m_DynamicArray.FindIndex(1, 2, (x) => x == 2));
            Assert.AreEqual(2, m_DynamicArray.FindIndex(1, 2, (x) => x == 3));
            Assert.AreEqual(-1, m_DynamicArray.FindIndex(1, 2, (x) => x == 4));
        }

        [Test]
        public void TestIndexOfReturnFirstValidIndex()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(2);

            Assert.AreEqual(1, m_DynamicArray.IndexOf(2));
        }

        [Test]
        public void TestIndexOfRangeRespectCount()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);

            Assert.AreEqual(-1, m_DynamicArray.IndexOf(4, 1, 2));
        }

        [Test]
        public void TestIndexOfRangeReturnFirstValidIndex()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(5);

            Assert.AreEqual(1, m_DynamicArray.IndexOf(4, 1, 3));
        }

        [Test]
        public void TestIndexOfWithStartingIndexFail()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(3);

            Assert.AreEqual(-1, m_DynamicArray.IndexOf(1, 1, 2));
        }

        [Test]
        public void TestResizeIncreaseCapacity()
        {
            m_DynamicArray.Resize(256);

            Assert.AreEqual(256, m_DynamicArray.capacity);
        }

        [Test]
        public void TestReserveKeepsSize()
        {
            var size = m_DynamicArray.size;
            m_DynamicArray.Reserve(256);

            Assert.AreEqual(256, m_DynamicArray.capacity);
            Assert.AreEqual(size, m_DynamicArray.size);
        }

        [Test]
        public void TestResizeAndClear()
        {
            // Uses the capacity only allocator
            m_DynamicArray = new DynamicArray<int>(64, false);

            Assert.AreEqual(0, m_DynamicArray.size);
            Assert.AreEqual(64, m_DynamicArray.capacity);

            // First resize it to 32 elements
            m_DynamicArray.Resize(32, false);
            Assert.AreEqual(32, m_DynamicArray.size);
            m_DynamicArray[31] = 0xFFFF;

            // Resize it back to 0 elements
            m_DynamicArray.Resize(0);
            Assert.AreEqual(0, m_DynamicArray.size);

            // Resize it back to 32 elements, the memory should not have been reallocated
            // it also shouln't have been cleared (for peformance reasons)
            m_DynamicArray.Resize(32);
            Assert.AreEqual(32, m_DynamicArray.size);
            Assert.AreEqual(0xFFFF, m_DynamicArray[31]);

            // Resize it back to 0 elements
            m_DynamicArray.Resize(0);
            Assert.AreEqual(0, m_DynamicArray.size);

            // Resize it back to 32 elements, the memory should not have been reallocated
            // the memory should have been cleared, resize and clear was used
            m_DynamicArray.ResizeAndClear(32);
            Assert.AreEqual(32, m_DynamicArray.size);
            Assert.AreEqual(0, m_DynamicArray[31]);
        }

        [Test]
        public void TestDynamicString()
        {
            var ds = new DynamicString("Foo");

            ds.Append("Bar");
            ds.Append(new DynamicString("Baz"));

            var s = ds.ToString();
            Assert.AreEqual("FooBarBaz", s);
        }

        [Test]
        public void TestAppendSelf()
        {
            var ds = new DynamicString("Foo");

            ds.Append(ds);

            var s = ds.ToString();
            Assert.AreEqual("FooFoo", s);
        }

        [Test]
        public void TestQuickSort()
        {
            m_DynamicArray.Add(8);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(5);
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(12);

            m_DynamicArray.QuickSort();

            Assert.AreEqual(1, m_DynamicArray[0]);
            Assert.AreEqual(3, m_DynamicArray[1]);
            Assert.AreEqual(4, m_DynamicArray[2]);
            Assert.AreEqual(4, m_DynamicArray[3]);
            Assert.AreEqual(5, m_DynamicArray[4]);
            Assert.AreEqual(8, m_DynamicArray[5]);
            Assert.AreEqual(12, m_DynamicArray[6]);
        }

        [Test]
        public void TestForEach_FullRange()
        {
            m_DynamicArray.Add(8);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(6);

            var sum = 0;
            foreach (var i in m_DynamicArray)
            {
                sum += i;
            }
            Assert.AreEqual(21, sum);

            // now check if we are _not_ iterating on empty array
            sum = 0;
            m_DynamicArray.Clear();
            foreach (var i in m_DynamicArray)
            {
                sum += i;
            }
            Assert.AreEqual(0, sum);
        }
        [Test]
        public void TestForEach_SubRange()
        {
            m_DynamicArray.Add(8);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(6);

            var sum = 0;
            foreach (var i in m_DynamicArray.SubRange(1,2))
            {
                sum += i;
            }

            Assert.AreEqual(7, sum);

            //remove item of array and test again
            m_DynamicArray.RemoveAt(1);
            sum = 0;
            foreach (var i in m_DynamicArray.SubRange(1,2))
            {
                sum += i;
            }
            Assert.AreEqual(9, sum);
        }

        [Test]
        public void GetEnumerators_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => {
                var iterator = new DynamicArray<int>.Iterator(null);
            });

            Assert.Throws<ArgumentNullException>(() => {
                var iterator = new DynamicArray<int>.RangeEnumerable.RangeIterator(null, 0,0);
            });

            m_DynamicArray.Add(12);
            m_DynamicArray.Add(2);

            Assert.Throws<IndexOutOfRangeException>(() => { var iterator = m_DynamicArray.SubRange(-5, 1); });
            Assert.Throws<IndexOutOfRangeException>(() => { var iterator = m_DynamicArray.SubRange(17, 1); });
            Assert.Throws<IndexOutOfRangeException>(() => { var iterator = m_DynamicArray.SubRange(0, 12); });
            Assert.Throws<IndexOutOfRangeException>(() => { var iterator = m_DynamicArray.SubRange(1, 2); });
            Assert.DoesNotThrow(() => { var iterator = m_DynamicArray.SubRange(1, 1); });
            Assert.Throws<IndexOutOfRangeException>(() => { var iterator = m_DynamicArray.SubRange(2, 1); });
        }

        [Test]
        public void Foreach_TestNoModificationsAllowed()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                m_DynamicArray.Add(3);
                m_DynamicArray.Add(3);

                foreach (var i in m_DynamicArray)
                {
                    m_DynamicArray.Remove(i);
                }
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                m_DynamicArray.Add(3);
                m_DynamicArray.Add(3);

                foreach (var i in m_DynamicArray)
                {
                    m_DynamicArray.Add(1);
                }
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                m_DynamicArray.Add(8);
                m_DynamicArray.Add(3);

                foreach (var i in m_DynamicArray)
                {
                    if (i == 3)
                    {
                        m_DynamicArray.Add(1);
                        m_DynamicArray.RemoveAt(0);
                    }
                }
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                m_DynamicArray.Add(8);
                m_DynamicArray.Add(3);
                m_DynamicArray.Add(7);

                foreach (var i in m_DynamicArray)
                {
                    if (i == 3)
                    {
                        m_DynamicArray.QuickSort();
                    }
                }
            });
        }

        static Recorder gcAllocRecorder = Recorder.Get("GC.Alloc");
        static int CountGCAllocs(Action action)
        {
            gcAllocRecorder.FilterToCurrentThread();
            gcAllocRecorder.enabled = false;
            gcAllocRecorder.enabled = true;

            action();

            gcAllocRecorder.enabled = false;
            return gcAllocRecorder.sampleBlockCount;
        }
        static void ValidateNoGCAllocs(Action action)
        {
            // warmup
            // this will catch static c'tors etc
            CountGCAllocs(action);

            // actual test
            var count = CountGCAllocs(action);
            if (count != 0)
                throw new AssertionException($"Expected 0 GC allocations but there were {count}");
        }

        [Test]
        public void Foreach_NoGC()
        {
            m_DynamicArray.Reserve(4);
            m_DynamicArray.Add(12);
            m_DynamicArray.Add(4);
            m_DynamicArray.Add(8);
            m_DynamicArray.Add(2);

            var sum = 0;
            ValidateNoGCAllocs(() =>
            {
                sum = 0;
                foreach (var i in m_DynamicArray)
                {
                     sum += i;
                }
            });
            Assert.AreEqual(sum, 26);

            ValidateNoGCAllocs(() =>
            {
                sum = 0;
                foreach (var i in m_DynamicArray.SubRange(0, 2))
                {
                    sum += i;
                }
            });
            Assert.AreEqual(sum, 16);
        }
    }
}
