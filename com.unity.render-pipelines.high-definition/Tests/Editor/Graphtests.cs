using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class GraphTests
    {
        static object[] k_ArrayListRefEnumerate =
        {
            new object[] { new List<int> {  } },
            new object[] { new List<int> { 1 } },
            new object[] { new List<int> { 1, 2 } },
            new object[] { new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 } },
        };

        [Test, TestCaseSource(nameof(k_ArrayListRefEnumerate))]
        public void ArrayValues_Works(List<int> list)
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
        public void ArrayValuesMut_Works(List<int> list)
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
    }
}
