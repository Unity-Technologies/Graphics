using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal.Tests
{
    public class UnsafeListExtensionTests : MonoBehaviour
    {
        [Test]
        public void UnsafeList_Reverse()
        {
            const int k_Elements = 4;
            UnsafeList<int> list = new UnsafeList<int>(k_Elements, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < k_Elements; i++)
                list.Add(i);

            list.Reverse();

            for (int i = 0; i < k_Elements; i++)
                Assert.IsTrue(list[i] == (k_Elements - i - 1));
        }


        [Test]
        public void UnsafeList_Insert()
        {
            int[] expectedValues = new int[] { -1, 0, 1, -1, 2, 3, -1 };

            const int k_Elements = 4;
            UnsafeList<int> list = new UnsafeList<int>(k_Elements, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < k_Elements; i++)
                list.Add(i);

            list.Insert(k_Elements, -1);
            list.Insert(2, -1);
            list.Insert(0, -1);

            for(int i=0; i < k_Elements+3; i++)
                Assert.IsTrue(list[i] == expectedValues[i]);
        }

        [Test]
        public void UnsafeList_Range()
        {
            int[] expectedValues = new int[] { 0, 1, 2, 3, 1, 2 };

            const int k_Elements = 4;
            UnsafeList<int> list = new UnsafeList<int>(k_Elements, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < k_Elements; i++)
                list.Add(i);

            UnsafeList<int> range = list.GetRange(1, 2);
            for (int i = 0; i < 2; i++)
                Assert.IsTrue(range[i] == i+1);

            list.AddRange(range);

            for (int i = 0; i < k_Elements + 2; i++)
                Assert.IsTrue(list[i] == expectedValues[i]);
        }
    }
}
