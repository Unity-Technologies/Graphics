using System;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph.UnitTests.Serialization
{
    public class ParsingTests
    {
        [Test]
        public void Empty()
        {
            var result = MultiJsonInternal.Parse("");
            Assert.IsEmpty(result);
        }

        [Test]
        public void Whitespace()
        {
            var result = MultiJsonInternal.Parse("          ");
            Assert.IsEmpty(result);
        }

        [Test]
        public void Single()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""m_Id"": ""0f8fad5b-d9cb-469f-a165-70867728950e"",
        ""RandomProperty"": 1234
    }
}";
            var expected = new[]
            {
                new MultiJsonEntry("UnityEditor.ShaderGraph.GraphData", "0f8fad5b-d9cb-469f-a165-70867728950e", str)
            };
            var result = MultiJsonInternal.Parse(str);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void SingleTrailingNewline()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""m_Id"": ""0f8fad5b-d9cb-469f-a165-70867728950e"",
        ""RandomProperty"": 1234
    }
}
";
            var expected = new[]
            {
                new MultiJsonEntry("UnityEditor.ShaderGraph.GraphData", "0f8fad5b-d9cb-469f-a165-70867728950e", str)
            };
            var result = MultiJsonInternal.Parse(str);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void SingleMissingId()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""RandomProperty"": 1234
    }
}";
            Assert.Throws<InvalidOperationException>(() => MultiJsonInternal.Parse(str));
        }

        [Test]
        public void SingleMissingType()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Id"": ""0f8fad5b-d9cb-469f-a165-70867728950e"",
        ""RandomProperty"": 1234
    }
}";
            Assert.Throws<InvalidOperationException>(() => MultiJsonInternal.Parse(str));
        }

        [Test]
        public void InvalidJson()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Id"": ""0f8fad5b-d9cb-469f-a165-70867728950e"",
        ""RandomProperty"" 1234
    }
}";
            Assert.Throws<ArgumentException>(() => MultiJsonInternal.Parse(str));
        }

        [Test]
        public void Multiple()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""m_Id"": ""0b1ec0af-bd71-4f2b-a2d2-d20bcd2185a4"",
        ""RandomProperty"": 1234
    }
}

{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.AddNode"",
        ""m_Id"": ""89b18fe3-43b5-4354-9cb1-d5e76c39988e"",
        ""AnotherRandomProperty"": ""test""
    }
}

{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.Vector4MaterialSlot"",
        ""m_Id"": ""a3734af1-64db-47ae-8a27-3ca24c2ff4b3"",
        ""More"": {
            ""Array"": [1, 2, 3, 4]
        }
    }
}
";
            var expected = new[]
            {
                new MultiJsonEntry("UnityEditor.ShaderGraph.GraphData", "0b1ec0af-bd71-4f2b-a2d2-d20bcd2185a4", @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""m_Id"": ""0b1ec0af-bd71-4f2b-a2d2-d20bcd2185a4"",
        ""RandomProperty"": 1234
    }
}"),
                new MultiJsonEntry("UnityEditor.ShaderGraph.AddNode", "89b18fe3-43b5-4354-9cb1-d5e76c39988e", @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.AddNode"",
        ""m_Id"": ""89b18fe3-43b5-4354-9cb1-d5e76c39988e"",
        ""AnotherRandomProperty"": ""test""
    }
}"),
                new MultiJsonEntry("UnityEditor.ShaderGraph.Vector4MaterialSlot", "a3734af1-64db-47ae-8a27-3ca24c2ff4b3", @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.Vector4MaterialSlot"",
        ""m_Id"": ""a3734af1-64db-47ae-8a27-3ca24c2ff4b3"",
        ""More"": {
            ""Array"": [1, 2, 3, 4]
        }
    }
}")
            };

            var result = MultiJsonInternal.Parse(str);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void MultipleMissingLineSpacing()
        {
            const string str = @"{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.GraphData"",
        ""m_Id"": ""0b1ec0af-bd71-4f2b-a2d2-d20bcd2185a4"",
        ""RandomProperty"": 1234
    }
}
{
    ""MonoBehaviour"": {
        ""m_Type"": ""UnityEditor.ShaderGraph.Vector4MaterialSlot"",
        ""m_Id"": ""a3734af1-64db-47ae-8a27-3ca24c2ff4b3"",
        ""More"": {
            ""Array"": [1, 2, 3, 4]
        }
    }
}
";

            Assert.Throws<ArgumentException>(() => MultiJsonInternal.Parse(str));
        }
    }
}
