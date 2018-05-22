using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    /*   [TestFixture]
       public class PropertyChunkTests
       {
           class TestPropertyChunch : PropertyChunk
           {
               public TestPropertyChunch(string propertyName, string propertyDescription, HideState hideState)
                   : base(propertyName, propertyDescription, hideState)
               {}

               public override string GetPropertyString()
               {
                   return propertyName;
               }
           }

           [TestFixtureSetUp]
           public void RunBeforeAnyTests()
           {
               Debug.unityLogger.logHandler = new ConsoleLogHandler();
           }

           [SetUp]
           public void TestSetUp()
           {
           }

           private const string kPropertyName = "ThePropertyName";
           private const string kPropertyDescription = "ThePropertyDescription";

           [Test]
           public void TestSimplePropertyChunkIsConstructedProperly()
           {
               var chunk = new TestPropertyChunch(kPropertyName, kPropertyDescription, PropertyChunk.HideState.Visible);
               Assert.AreEqual(kPropertyName, chunk.propertyName);
               Assert.AreEqual(kPropertyDescription, chunk.propertyDescription);
               Assert.AreEqual(kPropertyName, chunk.GetPropertyString());
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestColorChunkReturnsValidValues()
           {
               var expectedPropertyString = "ThePropertyName(\"ThePropertyDescription\", Color) = (1,0,0,1)";
               var chunk = new ColorPropertyChunk(kPropertyName, kPropertyDescription, Color.red, ColorPropertyChunk.ColorType.Default, PropertyChunk.HideState.Visible);
               Assert.AreEqual(kPropertyName, chunk.propertyName);
               Assert.AreEqual(kPropertyDescription, chunk.propertyDescription);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(Color.red, chunk.defaultColor);
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestFloatChunkReturnsValidValues()
           {
               var expectedPropertyString = "ThePropertyName(\"ThePropertyDescription\", Float) = 0.5";
               var chunk = new FloatPropertyChunk(kPropertyName, kPropertyDescription, 0.5f, PropertyChunk.HideState.Visible);
               Assert.AreEqual(kPropertyName, chunk.propertyName);
               Assert.AreEqual(kPropertyDescription, chunk.propertyDescription);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(0.5f, chunk.defaultValue);
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestVectorChunkReturnsValidValues()
           {
               var vector = new Vector4(0.2f, 0.4f, 0.66f, 1.0f);
               var expectedPropertyString = "ThePropertyName(\"ThePropertyDescription\", Vector) = (0.2,0.4,0.66,1)";
               var chunk = new VectorPropertyChunk(kPropertyName, kPropertyDescription, vector, PropertyChunk.HideState.Visible);
               Assert.AreEqual(kPropertyName, chunk.propertyName);
               Assert.AreEqual(kPropertyDescription, chunk.propertyDescription);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(vector, chunk.defaultValue);
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestTextureChunkReturnsValidValues()
           {
               var expectedPropertyString = "ThePropertyName(\"ThePropertyDescription\", 2D) = \"bump\" {}";
               var chunk = new TexturePropertyChunk(kPropertyName, kPropertyDescription, null, TextureType.Bump, PropertyChunk.HideState.Visible, TexturePropertyChunk.ModifiableState.Modifiable);
               Assert.AreEqual(kPropertyName, chunk.propertyName);
               Assert.AreEqual(kPropertyDescription, chunk.propertyDescription);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(null, chunk.defaultTexture);
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestTexturePropertyChunkGeneratesValidPropertyStringVisibleNotModifiable()
           {
               var expectedPropertyString = "[NonModifiableTextureData] ThePropertyName(\"ThePropertyDescription\", 2D) = \"gray\" {}";
               var chunk = new TexturePropertyChunk(kPropertyName, kPropertyDescription, null, TextureType.Gray, PropertyChunk.HideState.Visible, TexturePropertyChunk.ModifiableState.NonModifiable);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(TexturePropertyChunk.ModifiableState.NonModifiable, chunk.modifiableState);
               Assert.AreEqual(PropertyChunk.HideState.Visible, chunk.hideState);
           }

           [Test]
           public void TestTexturePropertyChunkGeneratesValidPropertyStringHiddenNotModifiable()
           {
               var expectedPropertyString = "[HideInInspector] [NonModifiableTextureData] ThePropertyName(\"ThePropertyDescription\", 2D) = \"white\" {}";
               var chunk = new TexturePropertyChunk(kPropertyName, kPropertyDescription, null, TextureType.White, PropertyChunk.HideState.Hidden, TexturePropertyChunk.ModifiableState.NonModifiable);
               Assert.AreEqual(expectedPropertyString, chunk.GetPropertyString());
               Assert.AreEqual(TexturePropertyChunk.ModifiableState.NonModifiable, chunk.modifiableState);
               Assert.AreEqual(PropertyChunk.HideState.Hidden, chunk.hideState);
           }
       }*/
}
