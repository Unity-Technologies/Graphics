using System;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;

namespace UnitTests.Serialization
{
    [TestFixture]
    class JsonStoreParserTests
    {
        [Test]
        public void ParsedItemEquals()
        {
            var item1 = new RawJsonStoreItem
            {
                typeFullName = typeof(GraphData).FullName,
                id = "0f8fad5b-d9cb-469f-a165-70867728950e",
                json = "{}"
            };
            var item2 = new RawJsonStoreItem
            {
                typeFullName = typeof(GraphData).FullName,
                id = "0f8fad5b-d9cb-469f-a165-70867728950e",
                json = "{}"
            };
            Assert.AreEqual(item1, item2);
        }

        [Test]
        public void Empty()
        {
            var result = JsonStoreFormat.Parse("");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Whitespace()
        {
            var result = JsonStoreFormat.Parse("          ");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Single()
        {
            var expected = new RawJsonStoreItem
            {
                typeFullName = typeof(GraphData).FullName,
                id = "0f8fad5b-d9cb-469f-a165-70867728950e",
                json = "{}"
            };
            var str = $"--- {expected.typeFullName} {expected.id}{Environment.NewLine}{expected.json}";
            var result = JsonStoreFormat.Parse(str);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0]);
        }

//        [Test]
//        public void
    }
}
