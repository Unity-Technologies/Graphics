using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.TestTools.Constraints;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    using Is = UnityEngine.TestTools.Constraints.Is;

    unsafe class FixedAllocatorTests
    {
        [Test]
        public void Ctor_Throws_WhenBufferIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var a = new FixedAllocator(null, 1); });
        }

        [Test]
        public void Ctor_Throws_WhenBufferIsTooSmall()
        {
            var ptr = stackalloc byte[1];
            Assert.Throws<ArgumentException>(() => { var a = new FixedAllocator(ptr, 1); });
        }

        [Test]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        [TestCase(128)]
        [TestCase(256)]
        [TestCase(512)]
        public void Allocate_Works(int bufferSize)
        {
            var size = MemoryUtilities.Pad((uint)bufferSize, 4)
                       + (int)FixedAllocator.OverheadSize
                       + (int)FixedAllocator.OverheadSizePerAllocation;
            var ptr = stackalloc byte[(int)size];

            FixedAllocator alloc = default;
            byte* allocatedPtr = default;
            Assert.That(() =>
            {
                alloc = new FixedAllocator(ptr, (ulong)size);
                allocatedPtr = (byte*)alloc.Allocate((ulong)bufferSize);
            }, Is.Not.AllocatingGCMemory());
            
            Assert.True(
               ptr <= allocatedPtr && allocatedPtr + bufferSize <= ptr + size,
               $"Allocated memory {(ulong)allocatedPtr} - {(ulong)(allocatedPtr + bufferSize)} " +
               $"is not in allocation range {(ulong)ptr} - {(ulong)(ptr + size)}"
            );

            Assert.Throws<OutOfMemoryException>(() => alloc.Allocate(1));
        }

        [Test]
        public void API_Throws_WhenCurrentThreadIsInvalid()
        {
            var bufferPtr = stackalloc byte[64];
            var alloc = new FixedAllocator(bufferPtr, 64);
            var allocatedPtr = alloc.Allocate(1);
            // Use a nullable type to have a reference type that can be shared in another thread.
            bool? allThrows = null;

            var thread = new Thread(StartThread);
            thread.Start();
            thread.Join();

            void StartThread()
            {
                Assert.Throws<InvalidOperationException>(() => alloc.Allocate(1));
                Assert.Throws<InvalidOperationException>(() => alloc.Deallocate(allocatedPtr));
                Assert.Throws<InvalidOperationException>(() => alloc.Reallocate(allocatedPtr, 1));

                allThrows = true;
            }

            Assert.True(allThrows);
        }

        [Test]
        public void API_Throws_WhenAddressIsInvalid()
        {
            var bufferPtr = stackalloc byte[64];
            var alloc = new FixedAllocator(bufferPtr, 64);

            Assert.Throws<ArgumentException>(() => alloc.Deallocate((void*)1)); // Outside allocated range
            Assert.Throws<ArgumentException>(() => alloc.Deallocate(bufferPtr + 1)); // Not padded
            Assert.Throws<ArgumentException>(() => alloc.Reallocate((void*)1, 1)); // Outside allocated range
            Assert.Throws<ArgumentException>(() => alloc.Reallocate(bufferPtr + 1, 1)); // Not padded
        }

        [Test]
        public void Reallocate_Works_WithSmallerSize()
        {
            var bufferPtr = stackalloc byte[72];

            FixedAllocator alloc = default;
            byte* ptr = default, newPtr = default;

            Assert.That(() =>
            {
                alloc = new FixedAllocator(bufferPtr, 72);

                ptr = (byte*)alloc.Allocate(32);
                for (byte i = 0; i < 32; ++i)
                    ptr[i] = i;

                newPtr = (byte*)alloc.Reallocate(ptr, 28);

            }, Is.Not.AllocatingGCMemory());
            
            Assert.AreEqual((ulong)ptr, (ulong)newPtr);
        }

        [Test]
        public void Reallocate_Works_WithBiggerSize()
        {
            var bufferPtr = stackalloc byte[96];

            FixedAllocator alloc = default;
            byte* ptr = default, newPtr = default;

            Assert.That(() =>
            {
                alloc = new FixedAllocator(bufferPtr, 96);

                ptr = (byte*)alloc.Allocate(16);
                for (byte i = 0; i < 16; ++i)
                    ptr[i] = i;

                newPtr = (byte*)alloc.Reallocate(ptr, 32);

            }, Is.Not.AllocatingGCMemory());

            for (byte i = 0; i < 16; ++i)
                Assert.AreEqual(i, newPtr[i]);
            for (byte i = 0; i < 16; ++i)
                Assert.AreEqual(0, newPtr[i + 16]);
        }
    }

    unsafe class ArrayListTests
    {
        public struct IntList
        {
            public List<int> Values;

            public IntList(List<int> values) => Values = values;

            public override string ToString()
                => string.Join(",", Values.Select(i => i.ToString()).ToArray());
        }

        static object[] k_ArrayListRefEnumerate =
        {
            new object[] { new IntList(new List<int> {  }) },
            new object[] { new IntList(new List<int> { 1 }) },
            new object[] { new IntList(new List<int> { 1, 2 }) },
            new object[] { new IntList(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }) },
        };

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void Values_Works(IntList intList)
        {
            var list = intList.Values;
            var buffer = stackalloc byte[1024];
            var alloc = new FixedAllocator(buffer, 1024);

            // Allocate the array and set its values
            ArrayList<int, FixedAllocator> arraylist = default;
            Assert.That(() =>
            {
                arraylist = ArrayList<int>.New(alloc);
                arraylist.GrowCapacity(list.Count);
                foreach (var i in list)
                    arraylist.Add(i);
            }, Is.Not.AllocatingGCMemory());

            // Get the enumerators
            Assert.AreEqual(arraylist.count, list.Count);
            var l = list.GetEnumerator();
            ArrayListRefEnumerator<int, FixedAllocator> a = default;
            Assert.That(() => a = arraylist.values, Is.Not.AllocatingGCMemory());

            // Compare the values of the enumerator to its baseline
            for (int c = arraylist.count, i = 0; i < c; ++i)
            {
                bool moveNext = false;
                Assert.That(() => moveNext = a.MoveNext(), Is.Not.AllocatingGCMemory());
                Assert.True(moveNext);
                Assert.True(l.MoveNext());
                Assert.AreEqual(a.current, l.Current);
            }
        }

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void ValuesMut_Works(IntList intList)
        {
            var list = intList.Values;
            var buffer = stackalloc byte[1024];
            var alloc = new FixedAllocator(buffer, 1024);

            // Allocate and push values in the list
            ArrayList<int, FixedAllocator> arraylist = default;
            Assert.That(() =>
            {
                arraylist = ArrayList<int>.New(alloc);
                arraylist.GrowCapacity(list.Count);
                foreach (var i in list)
                    arraylist.Add(i);
            }, Is.Not.AllocatingGCMemory());

            // Increment values in the list
            Assert.AreEqual(arraylist.count, list.Count);
            Assert.That(() =>
            {
                var a = arraylist.valuesMut;
                while (a.MoveNext()) a.current += 1;
            }, Is.Not.AllocatingGCMemory());

            // Read the list and assert the value
            {
                ArrayListRefEnumerator<int, FixedAllocator> a = default;
                var l = list.GetEnumerator();
                Assert.That(() => a = arraylist.values, Is.Not.AllocatingGCMemory());

                for (int c = arraylist.count, i = 0; i < c; ++i)
                {
                    bool aMoveNext = false;
                    Assert.That(() => aMoveNext = a.MoveNext(), Is.Not.AllocatingGCMemory());
                    Assert.True(l.MoveNext());
                    Assert.AreEqual(a.current, l.Current + 1);
                }
            }
            
        }

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void Get_Works(IntList intList)
        {
            var list = intList.Values;
            var buffer = stackalloc byte[1024];
            var alloc = new FixedAllocator(buffer, 1024);

            ArrayList<int, FixedAllocator> arraylist = default;
            Assert.That(() =>
            {
                arraylist = ArrayList<int>.New(alloc);
                arraylist.GrowCapacity(list.Count);
                foreach (var i in list)
                    arraylist.Add(i);
            }, Is.Not.AllocatingGCMemory());

            Assert.AreEqual(arraylist.count, list.Count);
            for (var i = 0; i < arraylist.count; ++i)
            {
                Assert.That(() =>
                {
                    arraylist.GetUnsafe(i);
                    arraylist.GetMutUnsafe(i);
                    arraylist.Get(i);
                    arraylist.GetMut(i);
                }, Is.Not.AllocatingGCMemory());

                Assert.AreEqual(arraylist.GetUnsafe(i), list[i]);
                Assert.AreEqual(arraylist.GetMutUnsafe(i), list[i]);
                Assert.AreEqual(arraylist.Get(i), list[i]);
                Assert.AreEqual(arraylist.GetMut(i), list[i]);
            }

            for (var i = -arraylist.count; i < 0; ++i)
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => arraylist.Get(i));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => arraylist.GetMut(i));
            }

            for (var i = arraylist.count; i < arraylist.count * 2; ++i)
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => arraylist.Get(i));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => arraylist.GetMut(i));
            }
        }

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void RemoveSwapBack_Works(IntList intList)
        {
            var list = intList.Values;
            var buffer = stackalloc byte[10000];
            var alloc = new FixedAllocator(buffer, 10000);

            for (var i = 0; i < list.Count; ++i)
            {
                var copy = new List<int>(list);

                ArrayList<int, FixedAllocator> arrayList = default, arrayListUnsafe = default;
                Assert.That(() =>
                {
                    // Copy the list in arrayList and arrayListUnsafe
                    arrayList = ArrayList<int>.New(alloc);
                    arrayList.GrowCapacity(list.Count);
                    foreach (var v in list)
                        arrayList.Add(v);

                    arrayListUnsafe = ArrayList<int>.New(alloc);
                    arrayListUnsafe.GrowCapacity(list.Count);
                    foreach (var v in list)
                        arrayListUnsafe.Add(v);

                    // Remove with both safe and unsafe methods
                    arrayList.RemoveSwapBackAt(i);
                    unsafe { arrayListUnsafe.RemoveSwapBackAtUnsafe(i); }
                }, Is.Not.AllocatingGCMemory());

                // Remove also in the copy which is the baseline
                copy[i] = copy[copy.Count - 1];
                copy.RemoveAt(copy.Count - 1);

                Assert.AreEqual(arrayList.count, copy.Count);
                for (var j = 0; j < arrayList.count; ++j)
                {
                    Assert.AreEqual(arrayList.GetUnsafe(j), copy[j]);
                    Assert.AreEqual(arrayList.GetMutUnsafe(j), copy[j]);
                    Assert.AreEqual(arrayList.Get(j), copy[j]);
                    Assert.AreEqual(arrayList.GetMut(j), copy[j]);
                }

                Assert.AreEqual(arrayListUnsafe.count, copy.Count);
                for (var j = 0; j < arrayListUnsafe.count; ++j)
                {
                    Assert.AreEqual(arrayListUnsafe.GetUnsafe(j), copy[j]);
                    Assert.AreEqual(arrayListUnsafe.GetMutUnsafe(j), copy[j]);
                    Assert.AreEqual(arrayListUnsafe.Get(j), copy[j]);
                    Assert.AreEqual(arrayListUnsafe.GetMut(j), copy[j]);
                }
            }
        }
    }
}
