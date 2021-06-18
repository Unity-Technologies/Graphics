using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    /*
   [TestFixture]
   public class PropertyGeneratorTests
   {
       [OneTimeSetUp]
       public void RunBeforeAnyTests()
       {
           Debug.unityLogger.logHandler = new ConsoleLogHandler();
       }

       private const string kPropertyName = "ThePropertyName";
       private const string kPropertyDescription = "ThePropertyDescription";

     [Test]
       public void TestCanAddPropertyChunkToPropertyGenerator()
       {
           var chunk = new FloatPropertyChunk(kPropertyName, kPropertyDescription, 0.5f, PropertyChunk.HideState.Visible);
           var generator = new PropertyCollector();
           generator.AddShaderProperty(chunk);

           Assert.AreNotEqual(string.Empty, generator.GetPropertiesBlock(0));
       }

       [Test]
       public void TestCanGetShaderStringWithIndentWorks()
       {
           var chunk = new FloatPropertyChunk(kPropertyName, kPropertyDescription, 0.5f, PropertyChunk.HideState.Visible);
           var generator = new PropertyCollector();
           generator.AddShaderProperty(chunk);

           Assert.AreEqual(0, generator.GetPropertiesBlock(0).Count(x => x == '\t'));
           Assert.AreEqual(1, generator.GetPropertiesBlock(1).Count(x => x == '\t'));
           Assert.AreEqual(2, generator.GetPropertiesBlock(2).Count(x => x == '\t'));
       }

       [Test]
       public void TestCanGetConfiguredTextureInfos()
       {
           var chunk = new TexturePropertyChunk(kPropertyName, kPropertyDescription, null, TextureType.Bump, PropertyChunk.HideState.Visible, TexturePropertyChunk.ModifiableState.Modifiable);
           var generator = new PropertyCollector();
           generator.AddShaderProperty(chunk);

           var infos = generator.GetConfiguredTexutres();
           Assert.AreEqual(1, infos.Count);
           Assert.AreEqual(kPropertyName, infos[0].name);
           Assert.AreEqual(0, infos[0].textureId);
           Assert.AreEqual(TexturePropertyChunk.ModifiableState.Modifiable, infos[0].modifiable);
       }
}*/
}
