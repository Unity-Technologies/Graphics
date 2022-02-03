using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        }

        [Test]
        public void TestFindIndexSuccessReturnRightFirstIndex()
        {
            m_DynamicArray.Add(1);
            m_DynamicArray.Add(2);
            m_DynamicArray.Add(3);
            m_DynamicArray.Add(2);

            Assert.AreEqual(1, m_DynamicArray.FindIndex(0, m_DynamicArray.size, (x) => x == 2));
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
    }
}
