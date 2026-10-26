using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using System.Linq;


namespace UnityEditor.ShaderGraph.UnitTests
{
    class ListUtilityTests
    {
        List<int> GetList()
        {
            return new List<int>()
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
            };
        }

        List<int> GetEmptyList()
        {
            return new List<int>();
        }

        bool ListEquals(List<int> list, params int[] expectedValues)
        {
            if (list.Count != expectedValues.Length)
                return false;
            for (int x = 0; x < list.Count; x++)
                if (list[x] != expectedValues[x])
                    return false;
            return true;
        }

        [Test]
        public void ListEnumerateRange()
        {
            Assert.IsTrue(ListEquals(GetList().EnumerateRange(2, 3).ToList(), 2, 3, 4));
            Assert.IsTrue(ListEquals(GetList().EnumerateRange(0, 1).ToList(), 0));
            Assert.IsTrue(ListEquals(GetList().EnumerateRange(7, 3).ToList(), 7, 8, 9));
            Assert.IsTrue(ListEquals(GetList().EnumerateRange(0, 10).ToList(), 0, 1, 2, 3, 4, 5, 6, 7, 8, 9));

            Assert.IsFalse(GetList().EnumerateRange(0, 0).Any());
            Assert.IsFalse(GetList().EnumerateRange(5, 0).Any());
            Assert.IsFalse(GetList().EnumerateRange(10, 0).Any());
            Assert.IsFalse(GetEmptyList().EnumerateRange(0, 0).Any());

            Assert.Throws<ArgumentOutOfRangeException>(() => GetList().EnumerateRange(-1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetList().EnumerateRange(6, -1));
            Assert.Throws<ArgumentException>(() => GetList().EnumerateRange(7, 10));
            Assert.Throws<ArgumentException>(() => GetList().EnumerateRange(11, 0));
            Assert.Throws<ArgumentException>(() => GetList().EnumerateRange(5, int.MaxValue));
        }

        [Test]
        public void ListRemoveAllFromRange()
        {
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => true, 0, 10) == 10);
                Assert.IsTrue(ListEquals(list));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => false, 0, 10) == 0);
                Assert.IsTrue(ListEquals(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => x == 3, 0, 6) == 1);
                Assert.IsTrue(ListEquals(list, 0, 1, 2, 4, 5, 6, 7, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => true, 3, 3) == 3);
                Assert.IsTrue(ListEquals(list, 0, 1, 2, 6, 7, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => x == 3, 4, 2) == 0);
                Assert.IsTrue(ListEquals(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => x < 5, 0, 10) == 5);
                Assert.IsTrue(ListEquals(list, 5, 6, 7, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => ((x & 0x01) == 0), 2, 8) == 4);
                Assert.IsTrue(ListEquals(list, 0, 1, 3, 5, 7, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => ((x & 0x01) == 0x01), 0, 8) == 4);
                Assert.IsTrue(ListEquals(list, 0, 2, 4, 6, 8, 9));
            }
            {
                var list = GetList();
                Assert.IsTrue(list.RemoveAllFromRange(x => ((x & 0x03) == 0x00), 0, 10) == 3);
                Assert.IsTrue(ListEquals(list, 1, 2, 3, 5, 6, 7, 9));
            }
            {
                var list = GetEmptyList();
                Assert.IsTrue(list.RemoveAllFromRange(x => true, 0, 0) == 0);
                Assert.IsTrue(ListEquals(list));
            }
            {
                var list = GetList();
                Assert.Throws<ArgumentException>(() => list.RemoveAllFromRange(x => true, 5, int.MaxValue));
                Assert.IsTrue(ListEquals(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            }
        }
    }
}
