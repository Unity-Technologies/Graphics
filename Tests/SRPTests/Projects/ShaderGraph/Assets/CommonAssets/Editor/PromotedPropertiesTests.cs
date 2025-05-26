using System.Collections.Generic;
using NUnit.Framework;


namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class PromotedPropertiesTests
    {        
        static string kGraphName = "Assets/CommonAssets/Graphs/PromotedProperties/PromotedProperties.shadergraph";
        GraphData m_Graph;
        string m_ShaderCode;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            m_ShaderCode = ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");
        }

        [Test]
        public void NestedPropertiesDidGenerate()
        {
            Assert.IsTrue(m_ShaderCode.Contains("[Toggle(_A)]_A(\"A\", Float) = 0"));
            Assert.IsTrue(m_ShaderCode.Contains("_B(\"B\", Vector, 4) = (1, 0, 0, 0)"));
            Assert.IsTrue(m_ShaderCode.Contains("_C(\"C\", Vector, 4) = (0, 1, 0, 0)"));
            Assert.IsTrue(m_ShaderCode.Contains("_D(\"D\", Vector, 4) = (1, 1, 1, 1)"));
        }
    }
}
