using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    /*  [TestFixture]
      public class PropertyNodeTests
      {
          private class TestPropertyNode : PropertyNode
          {
              public const string TestPropertyName = "TestName";

              public override PropertyType propertyType
              {
                  get { return PropertyType.Float; }
              }

              public override PreviewProperty GetPreviewProperty()
              {
                  return new PreviewProperty()
                  {
                      m_Name = TestPropertyName
                  };
              }
          }

          private UnityEngine.MaterialGraph.MaterialGraph m_Graph;
          private Vector1Node m_Vector1Node;
          private Vector2Node m_Vector2Node;
          private Vector3Node m_Vector3Node;
          private Vector4Node m_Vector4Node;
          private ColorNode m_ColorNode;
  //        private Texture2DNode m_TextureNode;
          private TestPropertyNode m_PropertyNode;

          private const string kPropertyName = "PropertyName";
          public const string kDescription = "NewDescription";

          [TestFixtureSetUp]
          public void RunBeforeAnyTests()
          {
              Debug.unityLogger.logHandler = new ConsoleLogHandler();
          }

          [SetUp]
          public void TestSetUp()
          {
              m_Graph = new UnityEngine.MaterialGraph.MaterialGraph();
              m_Vector1Node = new Vector1Node();
              m_Vector2Node = new Vector2Node();
              m_Vector3Node = new Vector3Node();
              m_Vector4Node = new Vector4Node();
              m_ColorNode = new ColorNode();
             // m_TextureNode = new Texture2DNode();
              m_PropertyNode = new TestPropertyNode();

              m_Graph.AddNode(m_Vector1Node);
              m_Graph.AddNode(m_Vector2Node);
              m_Graph.AddNode(m_Vector3Node);
              m_Graph.AddNode(m_Vector4Node);
              m_Graph.AddNode(m_ColorNode);
              m_Graph.AddNode(m_TextureNode);
              m_Graph.AddNode(m_PropertyNode);
          }

          /*  [Test]
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
            }*

          [Test]
          public void TestPropertyExposedOnSubgraphReturnsFalse()
          {
              var subGraph = new SubGraph();
              var subNode = new TestPropertyNode();
              subNode.exposedState = PropertyNode.ExposedState.Exposed;
              subGraph.AddNode(subNode);
              Assert.AreEqual(PropertyNode.ExposedState.NotExposed, subNode.exposedState);

              m_PropertyNode.exposedState = PropertyNode.ExposedState.Exposed;
              Assert.AreEqual(PropertyNode.ExposedState.Exposed, m_PropertyNode.exposedState);
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
              m_Vector1Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector1Node.propertyName
                  + "(\""
                  + m_Vector1Node.description
                  + "\", Float) = "
                  + m_Vector1Node.value
                  + Environment.NewLine;

              m_Vector1Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector1Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector1NodeGeneratesCorrectPropertyUsages()
          {
              m_Vector1Node.value = 0.6f;
              m_Vector1Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new ShaderGenerator();
              m_Vector1Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector1Node.precision
                  + " "
                  + m_Vector1Node.propertyName
                  + ";"
                  + Environment.NewLine;

              m_Vector1Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector1Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector2NodeTypeIsCorrect()
          {
              Assert.AreEqual(PropertyType.Vector2, m_Vector2Node.propertyType);
          }

          [Test]
          public void TestVector2NodeReturnsCorrectValue()
          {
              var value = new Vector2(0.6f, 0.7f);
              m_Vector2Node.value = value;
              Assert.AreEqual(value, m_Vector2Node.value);
          }

          [Test]
          public void TestVector2NodeReturnsPreviewProperty()
          {
              var value = new Vector2(0.6f, 0.7f);
              var props = new List<PreviewProperty>();
              m_Vector2Node.value = value;
              m_Vector2Node.CollectPreviewMaterialProperties(props);
              Assert.AreEqual(props.Count, 1);
              Assert.AreEqual(m_Vector2Node.propertyName, props[0].m_Name);
              Assert.AreEqual(m_Vector2Node.propertyType, props[0].m_PropType);
              Assert.AreEqual(value, m_Vector2Node.value);
          }

          [Test]
          public void TestVector2NodeGeneratesCorrectPropertyBlock()
          {
              var value = new Vector2(0.6f, 0.7f);
              m_Vector2Node.value = value;
              m_Vector2Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new PropertyGenerator();
              m_Vector2Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector2Node.propertyName
                  + "(\""
                  + m_Vector2Node.description
                  + "\", Vector) = ("
                  + m_Vector2Node.value.x
                  + ","
                  + m_Vector2Node.value.y
                  + ",0,0)"
                  + Environment.NewLine;

              m_Vector2Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector2Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector2NodeGeneratesCorrectPropertyUsages()
          {
              var value = new Vector2(0.6f, 0.7f);
              m_Vector2Node.value = value;
              m_Vector2Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new ShaderGenerator();
              m_Vector2Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector2Node.precision
                  + "2 "
                  + m_Vector2Node.propertyName
                  + ";"
                  + Environment.NewLine;

              m_Vector2Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector2Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector3NodeTypeIsCorrect()
          {
              Assert.AreEqual(PropertyType.Vector3, m_Vector3Node.propertyType);
          }

          [Test]
          public void TestVector3NodeReturnsCorrectValue()
          {
              var value = new Vector3(0.6f, 0.7f, 0.4f);
              m_Vector3Node.value = value;
              Assert.AreEqual(value, m_Vector3Node.value);
          }

          [Test]
          public void TestVector3NodeReturnsPreviewProperty()
          {
              var value = new Vector3(0.6f, 0.7f, 0.4f);
              var props = new List<PreviewProperty>();
              m_Vector3Node.value = value;
              m_Vector3Node.CollectPreviewMaterialProperties(props);
              Assert.AreEqual(props.Count, 1);
              Assert.AreEqual(m_Vector3Node.propertyName, props[0].m_Name);
              Assert.AreEqual(m_Vector3Node.propertyType, props[0].m_PropType);
              Assert.AreEqual(value, m_Vector3Node.value);
          }

          [Test]
          public void TestVector3NodeGeneratesCorrectPropertyBlock()
          {
              var value = new Vector3(0.6f, 0.7f, 0.4f);
              m_Vector3Node.value = value;
              m_Vector3Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new PropertyGenerator();
              m_Vector3Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector3Node.propertyName
                  + "(\""
                  + m_Vector3Node.description
                  + "\", Vector) = ("
                  + m_Vector3Node.value.x
                  + ","
                  + m_Vector3Node.value.y
                  + ","
                  + m_Vector3Node.value.z
                  + ",0)"
                  + Environment.NewLine;

              m_Vector3Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector3Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector3NodeGeneratesCorrectPropertyUsages()
          {
              var value = new Vector3(0.6f, 0.7f, 0.4f);
              m_Vector3Node.value = value;
              m_Vector3Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new ShaderGenerator();
              m_Vector3Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector3Node.precision
                  + "3 "
                  + m_Vector3Node.propertyName
                  + ";"
                  + Environment.NewLine;

              m_Vector3Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector3Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector4NodeTypeIsCorrect()
          {
              Assert.AreEqual(PropertyType.Vector4, m_Vector4Node.propertyType);
          }

          [Test]
          public void TestVector4NodeReturnsCorrectValue()
          {
              var value = new Vector4(0.6f, 0.7f, 0.4f, 0.3f);
              m_Vector4Node.value = value;
              Assert.AreEqual(value, m_Vector4Node.value);
          }

          [Test]
          public void TestVector4NodeReturnsPreviewProperty()
          {
              var value = new Vector4(0.6f, 0.7f, 0.4f, 0.3f);
              var props = new List<PreviewProperty>();
              m_Vector4Node.value = value;
              m_Vector4Node.CollectPreviewMaterialProperties(props);
              Assert.AreEqual(props.Count, 1);
              Assert.AreEqual(m_Vector4Node.propertyName, props[0].m_Name);
              Assert.AreEqual(m_Vector4Node.propertyType, props[0].m_PropType);
              Assert.AreEqual(value, m_Vector4Node.value);
          }

          [Test]
          public void TestVector4NodeGeneratesCorrectPropertyBlock()
          {
              var value = new Vector4(0.6f, 0.7f, 0.4f, 0.3f);
              m_Vector4Node.value = value;
              m_Vector4Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new PropertyGenerator();
              m_Vector4Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector4Node.propertyName
                  + "(\""
                  + m_Vector4Node.description
                  + "\", Vector) = ("
                  + m_Vector4Node.value.x
                  + ","
                  + m_Vector4Node.value.y
                  + ","
                  + m_Vector4Node.value.z
                  + ","
                  + m_Vector4Node.value.w
                  + ")"
                  + Environment.NewLine;

              m_Vector4Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector4Node.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestVector4NodeGeneratesCorrectPropertyUsages()
          {
              var value = new Vector4(0.6f, 0.7f, 0.4f, 0.3f);
              m_Vector4Node.value = value;
              m_Vector4Node.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new ShaderGenerator();
              m_Vector4Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_Vector4Node.precision
                  + "4 "
                  + m_Vector4Node.propertyName
                  + ";"
                  + Environment.NewLine;

              m_Vector4Node.exposedState = PropertyNode.ExposedState.Exposed;
              m_Vector4Node.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestColorNodeTypeIsCorrect()
          {
              Assert.AreEqual(PropertyType.Color, m_ColorNode.propertyType);
          }

          [Test]
          public void TestColorNodeReturnsCorrectValue()
          {
              var value = new Color(0.6f, 0.7f, 0.4f, 0.3f);
              m_ColorNode.color = value;
              Assert.AreEqual(value, m_ColorNode.color);
          }

          [Test]
          public void TestColorNodeReturnsPreviewProperty()
          {
              var value = new Color(0.6f, 0.7f, 0.4f, 0.3f);
              var props = new List<PreviewProperty>();
              m_ColorNode.color = value;
              m_ColorNode.CollectPreviewMaterialProperties(props);
              Assert.AreEqual(props.Count, 1);
              Assert.AreEqual(m_ColorNode.propertyName, props[0].m_Name);
              Assert.AreEqual(m_ColorNode.propertyType, props[0].m_PropType);
              Assert.AreEqual(value, m_ColorNode.color);
          }

          [Test]
          public void TestColorNodeGeneratesCorrectPropertyBlock()
          {
              var value = new Color(0.6f, 0.7f, 0.4f, 0.3f);
              m_ColorNode.color = value;
              m_ColorNode.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new PropertyGenerator();
              m_ColorNode.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_ColorNode.propertyName
                  + "(\""
                  + m_ColorNode.description
                  + "\", Color) = ("
                  + m_ColorNode.color.r
                  + ","
                  + m_ColorNode.color.g
                  + ","
                  + m_ColorNode.color.b
                  + ","
                  + m_ColorNode.color.a
                  + ")"
                  + Environment.NewLine;

              m_ColorNode.exposedState = PropertyNode.ExposedState.Exposed;
              m_ColorNode.GeneratePropertyBlock(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }

          [Test]
          public void TestColorNodeGeneratesCorrectPropertyUsages()
          {
              var value = new Color(0.6f, 0.7f, 0.4f, 0.3f);
              m_ColorNode.color = value;
              m_ColorNode.exposedState = PropertyNode.ExposedState.NotExposed;
              var generator = new ShaderGenerator();
              m_ColorNode.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(string.Empty, generator.GetShaderString(0));

              var expected = m_ColorNode.precision
                  + "4 "
                  + m_ColorNode.propertyName
                  + ";"
                  + Environment.NewLine;

              m_ColorNode.exposedState = PropertyNode.ExposedState.Exposed;
              m_ColorNode.GeneratePropertyUsages(generator, GenerationMode.ForReals);
              Assert.AreEqual(expected, generator.GetShaderString(0));
          }
      }*/
}
