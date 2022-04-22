using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ListExtensionsTests
    {
        [Test]
        [TestCase("ABC", "A", ReorderType.MoveDown, "BAC")]
        [TestCase("ABC", "B", ReorderType.MoveDown, "ACB")]
        [TestCase("ABC", "C", ReorderType.MoveDown, "ABC")]
        [TestCase("ABC", "AB", ReorderType.MoveDown, "CAB")]
        [TestCase("ABC", "BA", ReorderType.MoveDown, "CAB")]
        [TestCase("ABC", "AC", ReorderType.MoveDown, "BAC")]
        [TestCase("ABC", "CA", ReorderType.MoveDown, "BAC")]
        [TestCase("ABC", "BC", ReorderType.MoveDown, "ABC")]
        [TestCase("ABC", "CB", ReorderType.MoveDown, "ABC")]
        [TestCase("ABC", "ABC", ReorderType.MoveDown, "ABC")]
        [TestCase("ABC", "CBA", ReorderType.MoveDown, "ABC")]
        [TestCase("ABC", "A", ReorderType.MoveUp, "ABC")]
        [TestCase("ABC", "B", ReorderType.MoveUp, "BAC")]
        [TestCase("ABC", "C", ReorderType.MoveUp, "ACB")]
        [TestCase("ABC", "AB", ReorderType.MoveUp, "ABC")]
        [TestCase("ABC", "AC", ReorderType.MoveUp, "ACB")]
        [TestCase("ABC", "BC", ReorderType.MoveUp, "BCA")]
        [TestCase("ABC", "ABC", ReorderType.MoveUp, "ABC")]
        [TestCase("ABC", "A", ReorderType.MoveFirst, "ABC")]
        [TestCase("ABC", "B", ReorderType.MoveFirst, "BAC")]
        [TestCase("ABC", "C", ReorderType.MoveFirst, "CAB")]
        [TestCase("ABC", "AB", ReorderType.MoveFirst, "ABC")]
        [TestCase("ABC", "BA", ReorderType.MoveFirst, "ABC")]
        [TestCase("ABC", "AC", ReorderType.MoveFirst, "ACB")]
        [TestCase("ABC", "CA", ReorderType.MoveFirst, "ACB")]
        [TestCase("ABC", "BC", ReorderType.MoveFirst, "BCA")]
        [TestCase("ABC", "CB", ReorderType.MoveFirst, "BCA")]
        [TestCase("ABC", "ABC", ReorderType.MoveFirst, "ABC")]
        [TestCase("ABC", "CBA", ReorderType.MoveFirst, "ABC")]
        [TestCase("ABC", "A", ReorderType.MoveLast, "BCA")]
        [TestCase("ABC", "B", ReorderType.MoveLast, "ACB")]
        [TestCase("ABC", "C", ReorderType.MoveLast, "ABC")]
        [TestCase("ABC", "AB", ReorderType.MoveLast, "CAB")]
        [TestCase("ABC", "BA", ReorderType.MoveLast, "CAB")]
        [TestCase("ABC", "AC", ReorderType.MoveLast, "BAC")]
        [TestCase("ABC", "CA", ReorderType.MoveLast, "BAC")]
        [TestCase("ABC", "BC", ReorderType.MoveLast, "ABC")]
        [TestCase("ABC", "CB", ReorderType.MoveLast, "ABC")]
        [TestCase("ABC", "ABC", ReorderType.MoveLast, "ABC")]
        [TestCase("ABC", "CBA", ReorderType.MoveLast, "ABC")]
        [TestCase("ABCDE", "AB", ReorderType.MoveLast, "CDEAB")]
        [TestCase("ABCDE", "AD", ReorderType.MoveLast, "BCEAD")]
        [TestCase("ABCDE", "DE", ReorderType.MoveLast, "ABCDE")]
        [TestCase("ABCDE", "AD", ReorderType.MoveFirst, "ADBCE")]
        [TestCase("ABCDE", "DC", ReorderType.MoveFirst, "CDABE")]
        [TestCase("ABCDE", "AB", ReorderType.MoveDown, "CABDE")]
        [TestCase("ABCDE", "DC", ReorderType.MoveDown, "ABECD")]
        [TestCase("ABCDE", "BD", ReorderType.MoveDown, "ACBED")]
        public void ReorderElementsTest(string collection, string elementsToOrder, ReorderType reorderType,
            string expectedResult)
        {
            var list = collection.ToCharList();
            var elements = elementsToOrder.ToCharList();
            list.ReorderElements(elements, reorderType);
            Assert.AreEqual(string.Join("", list), expectedResult);
        }
    }

    static class TestStringExtensions
    {
        public static List<char> ToCharList(this string s)
        {
            return s.ToCharArray().ToList();
        }
    }
}
