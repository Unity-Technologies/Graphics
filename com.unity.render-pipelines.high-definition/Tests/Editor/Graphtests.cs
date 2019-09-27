using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ArrayListTests
    {
        static object[] k_ArrayListRefEnumerate =
        {
            new object[] { new List<int> {  } },
            new object[] { new List<int> { 1 } },
            new object[] { new List<int> { 1, 2 } },
            new object[] { new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 } },
        };

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void Values_Works(List<int> list)
        {
            var arraylist = new ArrayList<int>();
            foreach (var i in list)
                arraylist.Add(i);

            Assert.AreEqual(arraylist.count, list.Count);
            var a = arraylist.values;
            var l = list.GetEnumerator();
            for (int c = arraylist.count, i = 0; i < c; ++i)
            {
                Assert.True(a.MoveNext());
                Assert.True(l.MoveNext());
                Assert.AreEqual(a.current, l.Current);
            }
        }

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void ValuesMut_Works(List<int> list)
        {
            var arraylist = new ArrayList<int>();
            foreach (var i in list)
                arraylist.Add(i);

            Assert.AreEqual(arraylist.count, list.Count);

            {
                var a = arraylist.valuesMut;
                while (a.MoveNext()) a.current += 1;
            }

            {
                var a = arraylist.values;
                var l = list.GetEnumerator();
                for (int c = arraylist.count, i = 0; i < c; ++i)
                {
                    Assert.True(a.MoveNext());
                    Assert.True(l.MoveNext());
                    Assert.AreEqual(a.current, l.Current + 1);
                }
            }
        }

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void Get_Works(List<int> list)
        {
            var arraylist = new ArrayList<int>();
            foreach (var i in list)
                arraylist.Add(i);

            Assert.AreEqual(arraylist.count, list.Count);
            for (var i = 0; i < arraylist.count; ++i)
            {
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
        public void RemoveSwapBack_Works(List<int> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var copy = new List<int>(list);

                var arrayList = new ArrayList<int>();
                foreach (var v in list)
                    arrayList.Add(v);

                var arrayListUnsafe = new ArrayList<int>();
                foreach (var v in list)
                    arrayListUnsafe.Add(v);

                arrayList.RemoveSwapBackAt(i);
                unsafe { arrayListUnsafe.RemoveSwapBackAtUnsafe(i); }
                list[i] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);

                Assert.AreEqual(arrayList.count, list.Count);
                for (var j = 0; j < arrayList.count; ++j)
                {
                    Assert.AreEqual(arrayList.GetUnsafe(j), list[j]);
                    Assert.AreEqual(arrayList.GetMutUnsafe(j), list[j]);
                    Assert.AreEqual(arrayList.Get(j), list[j]);
                    Assert.AreEqual(arrayList.GetMut(j), list[j]);
                }

                Assert.AreEqual(arrayListUnsafe.count, list.Count);
                for (var j = 0; j < arrayListUnsafe.count; ++j)
                {
                    Assert.AreEqual(arrayListUnsafe.GetUnsafe(j), list[j]);
                    Assert.AreEqual(arrayListUnsafe.GetMutUnsafe(j), list[j]);
                    Assert.AreEqual(arrayListUnsafe.Get(j), list[j]);
                    Assert.AreEqual(arrayListUnsafe.GetMut(j), list[j]);
                }
            }

            var arraylist = new ArrayList<int>();
            foreach (var i in list)
                arraylist.Add(i);

            Assert.AreEqual(arraylist.count, list.Count);
            for (var i = 0; i < arraylist.count; ++i)
            {
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
    }
}
