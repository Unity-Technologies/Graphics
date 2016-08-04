using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.IntegrationTests
{

    [TestFixture]
    public class PropertyNodeTests
    {
        private PixelGraph m_Graph;
        private TextureNode m_TextureNode;
        
        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new PixelGraph();
            m_TextureNode = new TextureNode();
            m_Graph.AddNode(m_TextureNode);
        }

        [Test]
        public void TestTextureNodeTypeIsCorrect()
        {
            Assert.AreEqual(PropertyType.Texture2D, m_TextureNode.propertyType);
        }

        [Test]
        public void TestTextureNodeReturnsCorrectValue()
        {
            m_TextureNode.defaultTexture = null;
            Assert.AreEqual(null, m_TextureNode.defaultTexture);

            m_TextureNode.textureType = TextureType.Bump;
            Assert.AreEqual(TextureType.Bump, m_TextureNode.textureType);
        }

        [Test]
        public void TestTextureNodeReturnsPreviewProperty()
        {
            var props = new List<PreviewProperty>();
            m_TextureNode.defaultTexture = null;
            m_TextureNode.CollectPreviewMaterialProperties(props);
            Assert.AreEqual(props.Count, 1);
            Assert.AreEqual(m_TextureNode.propertyName, props[0].m_Name);
            Assert.AreEqual(m_TextureNode.propertyType, props[0].m_PropType);
            Assert.AreEqual(m_TextureNode.defaultTexture, props[0].m_Texture);
            Assert.AreEqual(null, m_TextureNode.defaultTexture);
        }

        [Test]
        public void TestTextureNodeGeneratesCorrectPropertyBlock()
        {
            m_TextureNode.defaultTexture = null;
            m_TextureNode.textureType = TextureType.Bump;
            m_TextureNode.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new PropertyGenerator();
            m_TextureNode.GeneratePropertyBlock(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(string.Empty, generator.GetShaderString(0));

            var expected = m_TextureNode.propertyName
                           + "(\""
                           + m_TextureNode.description
                           + "\", Texture) = ("

                           + ")"
                           + Environment.NewLine;

            m_TextureNode.exposedState = PropertyNode.ExposedState.Exposed;
            m_TextureNode.GeneratePropertyBlock(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, generator.GetShaderString(0));
        }

        [Test]
        public void TestTextureNodeGeneratesCorrectPropertyUsages()
        {
            m_TextureNode.defaultTexture = null;
            m_TextureNode.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new ShaderGenerator();
            m_TextureNode.GeneratePropertyUsages(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(string.Empty, generator.GetShaderString(0));

            var expected = m_TextureNode.precision
                           + "4 "
                           + m_TextureNode.propertyName
                           + ";"
                           + Environment.NewLine;

            m_TextureNode.exposedState = PropertyNode.ExposedState.Exposed;
            m_TextureNode.GeneratePropertyUsages(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, generator.GetShaderString(0));
        }
    }
}
