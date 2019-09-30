using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ScalableSettingSchemaTests
    {
        [Test]
        public void LevelNamesWorks()
        {
            var schema = ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels);

            Assert.AreEqual(3, schema.levelCount);
            Assert.AreEqual(3, schema.levelNames.Length);
            Assert.AreEqual("Low", schema.levelNames[0].text);
            Assert.AreEqual("Medium", schema.levelNames[1].text);
            Assert.AreEqual("High", schema.levelNames[2].text);
        }

        [Test]
        public void GetSchemaOrNullWorks()
        {
            {
                var schema = ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels);

                Assert.IsNotNull(schema);
                Assert.AreEqual(3, schema.levelCount);
                Assert.AreEqual(3, schema.levelNames.Length);
            }
            {
                ScalableSettingSchemaId? id = ScalableSettingSchemaId.With3Levels;
                var schema = ScalableSettingSchema.GetSchemaOrNull(id);

                Assert.IsNotNull(schema);
                Assert.AreEqual(3, schema.levelCount);
                Assert.AreEqual(3, schema.levelNames.Length);
            }
        }

        [Test]
        public void GetSchemaOrNull_ReturnsNullWhenMissing()
        {
            {
                var schema = ScalableSettingSchema.GetSchemaOrNull(default);
                Assert.IsNull(schema);
            }
            {
                ScalableSettingSchemaId? id = default;
                var schema = ScalableSettingSchema.GetSchemaOrNull(id);
                Assert.IsNull(schema);
            }
            {
                ScalableSettingSchemaId? id = null;
                var schema = ScalableSettingSchema.GetSchemaOrNull(id);
                Assert.IsNull(schema);
            }
        }
    }
}
