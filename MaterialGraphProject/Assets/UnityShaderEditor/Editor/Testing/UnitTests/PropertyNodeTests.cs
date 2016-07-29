using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class PropertyNodeTests
    {
        private class TestPropertyNode : PropertyNode
        {
            public const string TestPropertyName = "TestName";

            public override PropertyType propertyType
            {
                get { return PropertyType.Float;}
            }

            public override PreviewProperty GetPreviewProperty()
            {
                return new PreviewProperty()
                {
                    m_Name = TestPropertyName
                };
            }
        }
        
        private PixelGraph m_Graph;
        private Vector1Node m_Vector1Node;
        private Vector2Node m_Vector2Node;
        private Vector3Node m_Vector3Node;
        private Vector4Node m_Vector4Node;
        private ColorNode m_ColorNode;
        private TextureNode m_TextureNode;
        private TestPropertyNode m_PropertyNode;

        private const string kPropertyName = "PropertyName";
        public const string kDescription = "NewDescription";

        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new PixelGraph();
            m_Vector1Node = new Vector1Node();
            m_Vector2Node = new Vector2Node();
            m_Vector3Node = new Vector3Node();
            m_Vector4Node = new Vector4Node();
            m_ColorNode = new ColorNode();
            m_TextureNode = new TextureNode();
            m_PropertyNode = new TestPropertyNode();

            m_Graph.AddNode(m_Vector1Node);
            m_Graph.AddNode(m_Vector2Node);
            m_Graph.AddNode(m_Vector3Node);
            m_Graph.AddNode(m_Vector4Node);
            m_Graph.AddNode(m_ColorNode);
            m_Graph.AddNode(m_TextureNode);
            m_Graph.AddNode(m_PropertyNode);
        }
        
        [Test]
        public void TestExposedPropertyReturnsRawName()
        {
            m_PropertyNode.exposedState = PropertyNode.ExposedState.Exposed;
            m_PropertyNode.propertyName = kPropertyName;
            Assert.AreEqual(kPropertyName + "_Uniform", m_PropertyNode.propertyName);
        }

        [Test]
        public void TestNonExposedPropertyReturnsGeneratedName()
        {
            var expected = string.Format("{0}_{1}_Uniform", m_PropertyNode.name, m_PropertyNode.guid.ToString().Replace("-", "_"));
            m_PropertyNode.exposedState = PropertyNode.ExposedState.NotExposed;
            m_PropertyNode.propertyName = kPropertyName;

            Assert.AreEqual(expected, m_PropertyNode.propertyName);
        }

        [Test]
        public void TestPropertyNodeDescriptionWorks()
        {
            m_PropertyNode.propertyName = kPropertyName;
            m_PropertyNode.description = kDescription;
            Assert.AreEqual(kDescription, m_PropertyNode.description);
        }

        [Test]
        public void TestPropertyNodeDescriptionReturnsPropertyNameWhenNoDescriptionSet()
        {
            m_PropertyNode.propertyName = kPropertyName;
            m_PropertyNode.description = string.Empty;
            Assert.AreEqual(kPropertyName, m_PropertyNode.description);
        }

        [Test]
        public void TestPropertyNodeReturnsPreviewProperty()
        {
            var props = new List<PreviewProperty>();
            m_PropertyNode.CollectPreviewMaterialProperties(props);
            Assert.AreEqual(props.Count, 1);
            Assert.AreEqual(TestPropertyNode.TestPropertyName, props[0].m_Name);
        }

        [Test]
        public void TestDuplicatedPropertyNameGeneratesErrorWhenExposed()
        {
            const string failName = "SameName";

            m_Vector1Node.exposedState = PropertyNode.ExposedState.Exposed;
            m_Vector1Node.propertyName = failName;
            m_Vector2Node.exposedState = PropertyNode.ExposedState.Exposed;
            m_Vector2Node.propertyName = failName;

            m_Vector1Node.ValidateNode();
            m_Vector2Node.ValidateNode();
            Assert.IsTrue(m_Vector1Node.hasError);
            Assert.IsTrue(m_Vector2Node.hasError);
        }

        [Test]
        public void TestDuplicatedPropertyNameGeneratesNoErrorWhenNotExposed()
        {
            const string failName = "SameName";

            m_Vector1Node.exposedState = PropertyNode.ExposedState.NotExposed;
            m_Vector1Node.propertyName = failName;
            m_Vector2Node.exposedState = PropertyNode.ExposedState.Exposed;
            m_Vector2Node.propertyName = failName;

            m_Vector1Node.ValidateNode();
            m_Vector2Node.ValidateNode();
            Assert.IsFalse(m_Vector1Node.hasError);
            Assert.IsFalse(m_Vector2Node.hasError);
        }

        [Test]
        public void TestVector1NodeTypeIsCorrect()
        {
            Assert.AreEqual(PropertyType.Float, m_Vector1Node.propertyType);
        }

        [Test]
        public void TestVector1NodeReturnsCorrectValue()
        {
            m_Vector1Node.value = 0.6f;
            Assert.AreEqual(0.6f, m_Vector1Node.value);
        }

        [Test]
        public void TestVector1NodeReturnsPreviewProperty()
        {
            var props = new List<PreviewProperty>();
            m_Vector1Node.value = 0.6f;
            m_Vector1Node.CollectPreviewMaterialProperties(props);
            Assert.AreEqual(props.Count, 1);
            Assert.AreEqual(m_Vector1Node.propertyName, props[0].m_Name);
            Assert.AreEqual(m_Vector1Node.propertyType, props[0].m_PropType);
            Assert.AreEqual(0.6f, props[0].m_Float);
        }

        [Test]
        public void TestVector1NodeGeneratesCorrectPropertyBlock()
        {
            m_Vector1Node.value = 0.6f;
            m_Vector1Node.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new PropertyGenerator();
            m_Vector1Node.GeneratePropertyBlock(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(string.Empty, generator.GetShaderString(0));

            var expected = m_Vector1Node.propertyName
                           + "(\""
                           + m_Vector1Node.description
                           + "\", Float) = "
                           + m_Vector1Node.value
                           + "\n";

            m_Vector1Node.exposedState = PropertyNode.ExposedState.Exposed;
            m_Vector1Node.GeneratePropertyBlock(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, generator.GetShaderString(0));
        }

        [Test]
        public void TestVector1NodeGeneratesCorrectPropertyUsages()
        {
            m_Vector1Node.value = 0.6f;
            m_Vector1Node.exposedState = PropertyNode.ExposedState.NotExposed;
            var generator = new ShaderGenerator();
            m_Vector1Node.GeneratePropertyUsages(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(string.Empty, generator.GetShaderString(0));

            var expected = m_Vector1Node.precision
                           + " "
                           + m_Vector1Node.propertyName
                           + ";\r\n";

            m_Vector1Node.exposedState = PropertyNode.ExposedState.Exposed;
            m_Vector1Node.GeneratePropertyUsages(generator, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, generator.GetShaderString(0));
        }
    }
}
