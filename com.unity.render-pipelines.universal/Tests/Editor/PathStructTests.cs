using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    public class PathStructTests
    {
        const int k_TestListLen = 4;

        struct DisposableStruct : IDisposable
        {
            static public int disposeCount;

            public void Dispose()
            {
                disposeCount++;
            }
        }

        [Test]
        public void PathArray_CreateAndDestroy()
        {
            PathArray<int> pathArray0 = new PathArray<int>();
            PathArray<PathArray<int>> pathArray1 = new PathArray<PathArray<int>>();

            pathArray0.Dispose();
            pathArray1.Dispose();

            int startDisposes = DisposableStruct.disposeCount;
            PathArray<DisposableStruct> pathArray2 = new PathArray<DisposableStruct>(10);
            pathArray2.Dispose();
            int endDisposes = DisposableStruct.disposeCount;

            Assert.AreEqual(10, endDisposes - startDisposes);
        }

        [Test]
        public void PathArray_ReadAndWrite()
        {
            int[] expectedValues = new int[] { 0, 1, 2, 3 };

            PathArray<int> pathArray = new PathArray<int>(4);
            for (int i = 0; i < k_TestListLen; i++)
            {
                pathArray[i] = expectedValues[i];
            }

            for (int i = 0; i < k_TestListLen; i++)
            {
                Assert.IsTrue(pathArray[i] == expectedValues[i]);
            }

            pathArray.Dispose();
        }

        [Test]
        public void PathList_CreateAndDestroy()
        {
            PathList<int> pathList0 = new PathList<int>();
            PathList<int> pathList1 = new PathList<int>();
            PathList<PathList<int>> pathList2 = new PathList<PathList<int>>();

            PathList<int> pathList3 = new PathList<int>(100);
            Assert.IsTrue(pathList3.Capacity == 100);

            pathList0.Dispose();
            pathList1.Dispose();
            pathList2.Dispose();
            pathList3.Dispose();
        }

        [Test]
        public void PathList_Capacity()
        {
            PathList<int> pathList0 = new PathList<int>();
            int[] expectedValues = new int[] { 0, 1, 2, 3 };

            for (int i = 0; i < k_TestListLen - 1; i++)
                pathList0.Add(i);

            pathList0.Capacity = 100;

            pathList0.Add(3);

            Assert.IsTrue(pathList0.Capacity == 100);

            for (int i = 0; i < k_TestListLen; i++)
                Assert.IsTrue(pathList0[i] == expectedValues[i]);

            pathList0.Dispose();
        }

        [Test]
        public void PathList_TryAdjustCapacityRequirements()
        {
            PathList<int> pathList = new PathList<int>();

            Assert.IsTrue(pathList.Capacity == 0);
            pathList.TryAdjustCapacityRequirements();

            Assert.IsTrue(pathList.Capacity == 1);
            pathList.Add(0);

            pathList.TryAdjustCapacityRequirements();
            Assert.IsTrue(pathList.Capacity == 2);

            pathList.Dispose();
        }


        [Test]
        public void PathList_Count()
        {
            PathList<int> pathList0 = new PathList<int>();
            int[] expectedValues = new int[] { 0, 1, 2, 3 };

            for (int i = 0; i < k_TestListLen - 1; i++)
                pathList0.Add(i);

            pathList0.Capacity = 100;

            pathList0.Add(3);

            Assert.IsTrue(pathList0.Count == 4);

            pathList0.Dispose();
        }


        [Test]
        public void PathList_Clear()
        {
            int startDisposes = DisposableStruct.disposeCount;

            PathList<DisposableStruct> pathList0 = new PathList<DisposableStruct>();

            for (int i = 0; i < k_TestListLen - 1; i++)
                pathList0.Add(new DisposableStruct());

            pathList0.Capacity = 100;

            pathList0.Add(new DisposableStruct());

            pathList0.Clear();

            int totalDisposes = DisposableStruct.disposeCount - startDisposes;

            Assert.IsTrue(pathList0.Count == 0 && pathList0.Capacity == 100 && totalDisposes == k_TestListLen);

            pathList0.Dispose();
        }

        [Test]
        public void PathList_Add()
        {
            PathList<int> pathList = new PathList<int>();
            Assert.IsTrue(pathList.Capacity == 0);

            int[] expectedValues = new int[] { 0, 1, 2, 3 };
            int[] expectedCapacity = new int[] { 1, 2, 4, 4 };

            for (int i = 0; i < k_TestListLen; i++)
            {
                pathList.Add(i);
                Assert.IsTrue(pathList.Capacity == expectedCapacity[i]);
            }

            for (int i = 0; i < k_TestListLen; i++)
                Assert.IsTrue(pathList[i] == expectedValues[i]);

            pathList.Dispose();
        }

        [Test]
        public void PathList_Insert()
        {
            PathList<int> pathList = new PathList<int>();

            int[] insertOrder = new int[] { 0, 1, 1, 3 };
            int[] expectedValues = new int[] { 0, 2, 1, 3 };
            int[] expectedCapacity = new int[] { 1, 2, 4, 4 };

            for (int i = 0; i < k_TestListLen; i++)
            {
                pathList.Insert(insertOrder[i], i);
                Assert.IsTrue(pathList.Capacity == expectedCapacity[i]);
            }

            for (int i = 0; i < k_TestListLen; i++)
                Assert.IsTrue(pathList[i] == expectedValues[i]);

            pathList.Dispose();
        }

        [Test]
        public void PathList_ReverseSubset()
        {
            const int k_TestListLen = 6;

            PathList<int> pathList = new PathList<int>();
            int[] expectedValues = new int[] { 0, 4, 3, 2, 1, 5 };

            for (int i = 0; i < k_TestListLen; i++)
                pathList.Add(i);

            pathList.Reverse(1, 4);
            for (int i = 0; i < k_TestListLen; i++)
                Assert.IsTrue(pathList[i] == expectedValues[i]);

            pathList.Dispose();
        }

        [Test]
        public void PathList_Reverse()
        {
            PathList<int> pathList = new PathList<int>();
            int[] expectedValues = new int[] { 3, 2, 1, 0 };

            for (int i = 0; i < k_TestListLen; i++)
                pathList.Add(i);

            pathList.Reverse();
            for (int i = 0; i < k_TestListLen; i++)
            {
                int curValue = pathList[i];
                Assert.IsTrue(curValue == expectedValues[i]);
            }

            pathList.Dispose();
        }

        [Test]
        public void PathList_CopyTo_PathList()
        {
            const int k_TestListLen = 6;
            int[] expectedValues = new int[] { 0, 1, 1, 2, 3, 5 };

            PathList<int> pathListSource = new PathList<int>(k_TestListLen);
            PathList<int> pathListDest = new PathList<int>(k_TestListLen);

            for (int i = 0; i < k_TestListLen; i++)
            {
                pathListSource[i] = i;
                pathListDest[i] = i;
            }

            int[] array = new int[k_TestListLen];
            pathListSource.CopyTo(1, pathListDest, 2, 3);
            for (int i = 0; i < k_TestListLen; i++)
            {
                Assert.IsTrue(pathListDest[i] == expectedValues[i]);
            }

            pathListSource.Dispose();
            pathListDest.Dispose();
        }

        [Test]
        public void PathList_CopyTo_PathArray()
        {
            // Basic Copy
            PathList<int>   pathListSource = new PathList<int>(k_TestListLen);
            PathArray<int> pathArrayDest = new PathArray<int>(2 * k_TestListLen);

            int[] expectedValues = new int[] { 10, 0, 1, 2, 3, 10, 10, 10 };

            for (int i = 0; i < k_TestListLen; i++)
                pathListSource[i] = i;

            for (int i = 0; i < 2 * k_TestListLen; i++)
                pathArrayDest[i] = 10;

            pathListSource.CopyTo(pathArrayDest, 1);

            for (int i = 0; i < 2 * k_TestListLen; i++)
                Assert.IsTrue(pathArrayDest[i] == expectedValues[i]);

            pathListSource.Dispose();
            pathArrayDest.Dispose();
        }

        [Test]
        public void PathList_CopyTo_PathList_Append()
        {
            int[] expectedValues = new int[] { 0, 1, 2, 3, 0, 1, 2, 3 };

            PathList<int> pathListSource = new PathList<int>(k_TestListLen);
            PathList<int> pathListDest = new PathList<int>(k_TestListLen);

            for (int i = 0; i < k_TestListLen; i++)
            {
                pathListSource[i] = i;
                pathListDest[i] = i;
            }

            pathListSource.CopyTo(0, pathListDest, k_TestListLen, k_TestListLen);

            int[] array = new int[2 * k_TestListLen];
            for (int i = 0; i < 2*k_TestListLen; i++)
                array[i] = pathListDest[i];

            for (int i = 0; i < k_TestListLen; i++)
            {
                Assert.IsTrue(pathListDest[i] == expectedValues[i]);
                Assert.IsTrue(pathListDest[i+k_TestListLen] == expectedValues[i]);
            }

            pathListSource.Dispose();
            pathListDest.Dispose();
        }

        [Test]
        public void PathList_RemoveAt()
        {
            PathList<int> pathList0 = new PathList<int>();
            PathList<int> pathList1 = new PathList<int>();
            PathList<int> pathList2 = new PathList<int>();

            int[] expectedValues0 = new int[] { 1, 2, 3};
            int[] expectedValues1 = new int[] { 0, 1, 2 };
            int[] expectedValues2 = new int[] { 0, 2, 3 };

            for (int i = 0; i < k_TestListLen; i++)
            {
                pathList0.Add(i);
                pathList1.Add(i);
                pathList2.Add(i);
            }

            pathList0.RemoveAt(0);
            pathList1.RemoveAt(3);
            pathList2.RemoveAt(1);

            Assert.IsTrue(pathList0.Count == k_TestListLen - 1);
            Assert.IsTrue(pathList1.Count == k_TestListLen - 1);
            Assert.IsTrue(pathList2.Count == k_TestListLen - 1);

            for (int i=0;i < k_TestListLen-1;i++)
            {
                Assert.IsTrue(pathList0[i] == expectedValues0[i]);
                Assert.IsTrue(pathList1[i] == expectedValues1[i]);
                Assert.IsTrue(pathList2[i] == expectedValues2[i]);
            }
        }

        [Test]
        public void PathList_SharedData()
        {
            PathList<int> pathList0 = new PathList<int>(10);
            PathList<int> pathList1 = pathList0;

            pathList1.Capacity = 100;

            int foo = pathList0.Capacity;

            Assert.IsTrue(pathList0.Capacity == 100);
        }

    }
}
