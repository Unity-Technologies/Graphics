using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ScalableSettingValueTests
    {
        [Test]
        public void ValueWorkWithOverride()
        {
            var setting = new ScalableSetting<int>(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);
            var value = new ScalableSettingValue<int>
            {
                useOverride = true,
                @override = 4
            };

            Assert.AreEqual(4, value.Value(setting));
        }

        [Test]
        public void ValueWorkWithLevel()
        {
            var setting = new ScalableSetting<int>(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);
            var value = new ScalableSettingValue<int>
            {
                useOverride = false,
                @override = 4,
                level = 1,
            };

            Assert.AreEqual(2, value.Value(setting));
        }

        [Test]
        public void CopyToWorks()
        {
            var value1 = new ScalableSettingValue<int>
            {
                useOverride = false,
                @override = 4,
                level = 1,
            };

            var value2 = new ScalableSettingValue<int>();

            value1.CopyTo(value2);

            Assert.AreEqual(value1.level, value2.level);
            Assert.AreEqual(value1.useOverride, value2.useOverride);
            Assert.AreEqual(value1.@override, value2.@override);
        }
    }
}
