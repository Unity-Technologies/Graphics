using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ScalableSettingTests
    {
        [Test]
        public void IndexValuesWorks()
        {
            var setting = new ScalableSetting<int>(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);

            Assert.AreEqual(1, setting[0]);
            Assert.AreEqual(2, setting[1]);
            Assert.AreEqual(3, setting[2]);
            Assert.AreEqual(0, setting[3]);
            Assert.AreEqual(0, setting[4]);
        }

        [Test]
        public void ConstructorThrowForNullArray()
        {
            Assert.Throws<Assertions.AssertionException>(() => new ScalableSetting<int>(null, ScalableSettingSchemaId.With3Levels));
        }

        [Test]
        public void SchemaPropertyWorks()
        {
            var setting = new ScalableSetting<int>(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);

            Assert.AreEqual(ScalableSettingSchemaId.With3Levels, setting.schemaId);

            setting.schemaId = ScalableSettingSchemaId.With4Levels;
            Assert.AreEqual(ScalableSettingSchemaId.With4Levels, setting.schemaId);
        }

        [Test]
        public void TryGetWorks()
        {
            var setting = new ScalableSetting<int>(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);

            Assert.True(setting.TryGet(0, out var v));
            Assert.AreEqual(1, v);

            Assert.False(setting.TryGet(-1, out v));
            Assert.AreEqual(default(int), v);

            Assert.True(setting.TryGet(2, out v));
            Assert.AreEqual(3, v);

            Assert.False(setting.TryGet(3, out v));
            Assert.AreEqual(default(int), v);
        }
    }
}
