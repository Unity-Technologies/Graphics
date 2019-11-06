using System;
using NUnit;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;

namespace UnitTests.Serialization
{
    [TestFixture]
    class JsonAssetTests
    {
        [Test]
        public void ParsedItemEquals()
        {
            var item1 = new RawJsonObject
            {
                typeFullName = typeof(GraphData).FullName,
                id = "0f8fad5b-d9cb-469f-a165-70867728950e",
                json = "{}"
            };
            var item2 = new RawJsonObject
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
            Assert.Throws<InvalidOperationException>(() => JsonAsset.Parse(""), "Expected '--- '");
        }

        [Test]
        public void Whitespace()
        {
            Assert.Throws<InvalidOperationException>(() => JsonAsset.Parse("          "), "Expected '--- '");
        }

        [Test]
        public void Single()
        {
            var expected = new[]
            {
                new RawJsonObject
                {
                    typeFullName = typeof(GraphData).FullName,
                    id = "0f8fad5b-d9cb-469f-a165-70867728950e",
                    json = "not actual json"
                }
            };
            var str = $"--- {expected[0].typeFullName} {expected[0].id}{Environment.NewLine}{expected[0].json}";
            var result = JsonAsset.Parse(str);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Multiple()
        {
            var expected = new []
            {
                new RawJsonObject
                {
                    typeFullName = typeof(GraphData).FullName,
                    id = "0b1ec0af-bd71-4f2b-a2d2-d20bcd2185a4",
                    json = "here be json"
                },
                new RawJsonObject
                {
                    typeFullName = typeof(AddNode).FullName,
                    id = "89b18fe3-43b5-4354-9cb1-d5e76c39988e",
                    json = "{a value goes here}"
                },
                new RawJsonObject
                {
                    typeFullName = typeof(Vector4MaterialSlot).FullName,
                    id = "a3734af1-64db-47ae-8a27-3ca24c2ff4b3",
                    json = $"{{{Environment.NewLine}    \"value\": 1234{Environment.NewLine}}}"
                },
            };
            var str = "";
            foreach (var rawJsonObject in expected)
            {
                str += $"--- {rawJsonObject.typeFullName} {rawJsonObject.id}{Environment.NewLine}{rawJsonObject.json}{Environment.NewLine}{Environment.NewLine}";
            }
            var result = JsonAsset.Parse(str);
            Assert.AreEqual(expected, result);
        }

//        [Test]
//        public void
    }
}
