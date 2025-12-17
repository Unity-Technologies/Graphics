using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    class ParallelSortTests
    {
        const int kSmallArraySize = 17;
        const int kLargeArraySize = ParallelSortExtensions.kMinRadixSortArraySize * 3;

        private struct RNG
        {
            public ulong seed;

            public ulong GetNext()
            {
                seed = seed * 1664525 + 1013904223;
                return seed;
            }
        }

        static void VerifyOrder<T>(NativeList<T> array) where T : unmanaged, IComparable<T>
        {
            T prevValue = array[0];

            for (int i = 1; i < array.Length; i++)
            {
                var value = array[i];
                Assert.IsTrue(value.CompareTo(prevValue) >= 0);
                prevValue = value;
            }
        }

        void TestParallelSortInt(RNG data, NativeList<int> array, int size)
        {
            array.Clear();
            for (int i = 0; i < size; i++)
            {
                array.Add((int)data.GetNext());
            }
            array.AsArray().ParallelSort().Complete();
            VerifyOrder(array);
        }

        void TestParallelSortULong(RNG data, NativeList<ulong> array, int size)
        {
            array.Clear();
            for (int i = 0; i < size; i++)
            {
                array.Add(data.GetNext());
            }
            array.AsArray().ParallelSort().Complete();
            VerifyOrder(array);
        }

        [Test]
        public void TestParallelSortInt()
        {
            NativeList<int> array = new NativeList<int>(Allocator.Temp);
            RNG data = new RNG { seed = 12345678 };

            TestParallelSortInt(data, array, 1);
            TestParallelSortInt(data, array, kSmallArraySize);
            TestParallelSortInt(data, array, kLargeArraySize);

            array.Dispose();
        }

        [Test]
        public void TestParallelSortULong()
        {
            NativeList<ulong> array = new NativeList<ulong>(Allocator.Temp);
            RNG data = new RNG { seed = 12345678 };

            TestParallelSortULong(data, array, 1);
            TestParallelSortULong(data, array, kSmallArraySize);
            TestParallelSortULong(data, array, kLargeArraySize);

            array.Dispose();
        }
    }
}
