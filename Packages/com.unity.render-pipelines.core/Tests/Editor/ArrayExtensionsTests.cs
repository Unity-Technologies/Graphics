using NUnit.Framework;
using Unity.Collections;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.Tests
{
    class ArrayExtensionsTests
    {
        static NativeArray<int> CreateSequentialNativeArray(int length)
        {
            var array = new NativeArray<int>(length, Allocator.Persistent);
            for (int i = 0; i < length; ++i)
                array[i] = i + 1;
            return array;
        }

        [TestCase(5, 8)]
        [TestCase(10, 5)]
        [TestCase(10, 10)]
        [TestCase(10, 0)]
        public void ResizeNativeArray_KeepsLeadingElements(int initialLength, int newLength)
        {
            var array = CreateSequentialNativeArray(initialLength);
            try
            {
                array.ResizeArray(newLength);

                Assert.IsTrue(array.IsCreated);
                Assert.AreEqual(newLength, array.Length);
                int keptLength = Mathf.Min(initialLength, newLength);
                for (int i = 0; i < keptLength; ++i)
                    Assert.AreEqual(i + 1, array[i]);
            }
            finally
            {
                if (array.IsCreated)
                    array.Dispose();
            }
        }

        [Test]
        public void ResizeNativeArray_WhenNotCreated_AllocatesNewArray()
        {
            var array = default(NativeArray<int>);
            try
            {
                array.ResizeArray(4);

                Assert.IsTrue(array.IsCreated);
                Assert.AreEqual(4, array.Length);
            }
            finally
            {
                if (array.IsCreated)
                    array.Dispose();
            }
        }

        [TestCase(3, 5)]
        [TestCase(5, 3)]
        public void ResizeTransformAccessArray_KeepsLeadingTransforms(int initialLength, int newCapacity)
        {
            var gameObjects = new GameObject[initialLength];
            var array = new TransformAccessArray(initialLength);
            try
            {
                for (int i = 0; i < initialLength; ++i)
                {
                    gameObjects[i] = new GameObject($"ResizeTransformAccessArray_{i}");
                    array.Add(gameObjects[i].transform);
                }

                array.ResizeArray(newCapacity);

                int expectedLength = Mathf.Min(initialLength, newCapacity);
                Assert.AreEqual(expectedLength, array.length);
                for (int i = 0; i < expectedLength; ++i)
                    Assert.AreEqual(gameObjects[i].transform, array[i]);
            }
            finally
            {
                if (array.isCreated)
                    array.Dispose();
                foreach (var go in gameObjects)
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ResizeManagedArray_KeepsLeadingElements()
        {
            var array = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            ArrayExtensions.ResizeArray(ref array, 5);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, array);
        }
    }
}
