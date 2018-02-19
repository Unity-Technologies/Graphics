using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.IntegrationTests
{
    /*[TestFixture]
    public class PropertyNodeTests
    {
        private UnityEngine.MaterialGraph.MaterialGraph m_Graph;
        private Texture2DNode m_TextureNode;

        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        private static readonly string[] s_TexturePath =
        {
            "Assets",
            "UnityShaderEditor",
            "Editor",
            "Testing",
            "IntegrationTests",
            "Textures",
            "MudDiffuse.tif"
        };

        private static Texture FindTestTexture()
        {
            var texturePath = s_TexturePath.Aggregate(Path.Combine);
            return AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new UnityEngine.MaterialGraph.MaterialGraph();
            m_TextureNode = new Texture2DNode();
            m_Graph.AddNode(m_TextureNode);
        }

        [Test]
        public void TestTextureNodeTypeIsCorrect()
        {
            Assert.AreEqual(PropertyType.Texture, m_TextureNode.propertyType);
        }

        [Test]
        public void TestTextureNodeReturnsCorrectValue()
        {
            m_TextureNode.defaultTexture = FindTestTexture();
            Assert.AreEqual(FindTestTexture(), m_TextureNode.defaultTexture);

            m_TextureNode.textureType = TextureType.Bump;
            Assert.AreEqual(TextureType.Bump, m_TextureNode.textureType);
        }

        [Test]
        public void TestTextureNodeReturnsPreviewProperty()
        {
            var props = new List<PreviewProperty>();
            m_TextureNode.defaultTexture = FindTestTexture();
            m_TextureNode.CollectPreviewMaterialProperties(props);
            Assert.AreEqual(1, props.Count);
            Assert.AreEqual(m_TextureNode.propertyName, props[0].m_Name);
            Assert.AreEqual(m_TextureNode.propertyType, props[0].m_PropType);
            Assert.AreEqual(m_TextureNode.defaultTexture, props[0].m_Texture);
            Assert.AreEqual(FindTestTexture(), m_TextureNode.defaultTexture);
        }

        [Test]
        public void TestTextureNodeGeneratesCorrectPropertyBlock()
        {
            m_TextureNode.defaultTexture = null;
            m_TextureNode.textureType = TextureType.Bump;
            m_TextureNode.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new PropertyGenerator();
            m_TextureNode.GeneratePropertyBlock(generator, GenerationMode.ForReals);

            var expected1 = "[NonModifiableTextureData] "
                + m_TextureNode.propertyName
                + "(\""
                + m_TextureNode.description
                + "\", 2D) = \"bump\" {}"
                + Environment.NewLine;
            Assert.AreEqual(expected1, generator.GetShaderString(0));

            var expected2 = m_TextureNode.propertyName
                + "(\""
                + m_TextureNode.description
                + "\", 2D) = \"bump\" {}"
                + Environment.NewLine;
            m_TextureNode.exposedState = PropertyNode.ExposedState.Exposed;
            generator = new PropertyGenerator();
            m_TextureNode.GeneratePropertyBlock(generator, GenerationMode.ForReals);
            Assert.AreEqual(expected2, generator.GetShaderString(0));
        }

        [Test]
        public void TestTextureNodeGeneratesCorrectPropertyUsages()
        {
            m_TextureNode.defaultTexture = null;
            m_TextureNode.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new ShaderGenerator();
            m_TextureNode.GeneratePropertyUsages(generator, GenerationMode.ForReals);
            var expected = "UNITY_DECLARE_TEX2D("
                + m_TextureNode.propertyName
                + ");"
                + Environment.NewLine;
            Assert.AreEqual(expected, generator.GetShaderString(0));

            m_TextureNode.exposedState = PropertyNode.ExposedState.Exposed;
            generator = new ShaderGenerator();
            m_TextureNode.GeneratePropertyUsages(generator, GenerationMode.ForReals);
            Assert.AreEqual(expected, generator.GetShaderString(0));
        }
    }*/
}
