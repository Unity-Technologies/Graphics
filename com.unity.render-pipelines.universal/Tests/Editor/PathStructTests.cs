using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    public class PathStructTests
    {
        const int k_TestListLen = 4;

        [Test]
        public void PathArray_CreateAndDestroy()
        {
            PathArray<int> pathArray = new PathArray<int>();
            pathArray.Dispose();
        }

        [Test]
        public void PathArray_ReadAndWrite()
        {
            int[] expectedValues = new int[] { 0, 1, 2, 3 };

            PathArray<int> pathArray = new PathArray<int>(4);
            for(int i=0;i < k_TestListLen; i++)
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

            pathList0.Dispose();
            pathList1.Dispose();
            pathList2.Dispose();
        }

        [Test]
        public void PathList_CheckAdd()
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
            {
                Assert.IsTrue(pathList[i] == expectedValues[i]);
            }

            pathList.Dispose();
        }

        [Test]
        public void PathList_CheckInsert()
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
            {
                Assert.IsTrue(pathList[i] == expectedValues[i]);
            }

            pathList.Dispose();
        }
    }
}
