using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class SerializedScalableSettingValueTests
    {
        class ValueUsage : ScriptableObject
        {
            public IntScalableSettingValue intValue = new IntScalableSettingValue();
        }

        class ValueDeclaration : ScriptableObject
        {
            public IntScalableSetting intValue = new IntScalableSetting(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);
        }

        [Test]
        public void TryGetValueWorks_ForExistingLevelValue()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var usage = ScriptableObject.CreateInstance<ValueUsage>();
            usage.intValue.useOverride = false;
            usage.intValue.level = 1;

            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            var soUsage = new SerializedObject(usage);
            var serUsage = new SerializedScalableSettingValue(soUsage.FindProperty(nameof(ValueUsage.intValue)));

            Assert.True(serUsage.TryGetValue(serDecl, out int v));
            Assert.AreEqual(2, v);

            Assert.True(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(2, v);
        }

        [Test]
        public void TryGetValueWorks_ForOverrideValue()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var usage = ScriptableObject.CreateInstance<ValueUsage>();
            usage.intValue.useOverride = true;
            usage.intValue.@override = 5;

            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            var soUsage = new SerializedObject(usage);
            var serUsage = new SerializedScalableSettingValue(soUsage.FindProperty(nameof(ValueUsage.intValue)));

            Assert.True(serUsage.TryGetValue(serDecl, out int v));
            Assert.AreEqual(5, v);

            Assert.True(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(5, v);
        }

        [Test]
        public void TryGetValueWorks_ForMissingLevelValue()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var usage = ScriptableObject.CreateInstance<ValueUsage>();
            usage.intValue.useOverride = false;
            usage.intValue.level = 10;

            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            var soUsage = new SerializedObject(usage);
            var serUsage = new SerializedScalableSettingValue(soUsage.FindProperty(nameof(ValueUsage.intValue)));

            Assert.False(serUsage.TryGetValue(serDecl, out int v));
            Assert.AreEqual(default(int), v);

            Assert.False(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(default(int), v);
        }

        [Test]
        public void TryGetValueWorks_ForMultipleDifferentValues()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var usages = new[]
            {
                ScriptableObject.CreateInstance<ValueUsage>(),
                ScriptableObject.CreateInstance<ValueUsage>(),
            };
            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            var soUsage = new SerializedObject(usages);
            var serUsage = new SerializedScalableSettingValue(soUsage.FindProperty(nameof(ValueUsage.intValue)));

            // Different level values
            usages[0].intValue.useOverride = false;
            usages[0].intValue.level = 1;
            usages[1].intValue.useOverride = false;
            usages[1].intValue.level = 2;
            soUsage.SetIsDifferentCacheDirty();
            soUsage.Update();

            Assert.True(serUsage.hasMultipleValues);
            Assert.False(serUsage.TryGetValue(serDecl, out int v));
            Assert.AreEqual(default(int), v);

            Assert.False(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(default(int), v);

            // Different use override values
            usages[0].intValue.useOverride = true;
            usages[0].intValue.level = 1;
            usages[1].intValue.useOverride = false;
            usages[1].intValue.level = 1;
            soUsage.SetIsDifferentCacheDirty();
            soUsage.Update();

            Assert.True(serUsage.hasMultipleValues);
            Assert.False(serUsage.TryGetValue(serDecl, out v));
            Assert.AreEqual(default(int), v);

            Assert.False(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(default(int), v);

            // Different override values
            usages[0].intValue.useOverride = true;
            usages[0].intValue.@override = 5;
            usages[1].intValue.useOverride = true;
            usages[1].intValue.@override = 6;
            soUsage.SetIsDifferentCacheDirty();
            soUsage.Update();

            Assert.True(serUsage.hasMultipleValues);
            Assert.False(serUsage.TryGetValue(serDecl, out v));
            Assert.AreEqual(default(int), v);

            Assert.False(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(default(int), v);
        }

        [Test]
        public void TryGetValueWorks_ForMultipleIdenticalValues()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var usages = new[]
            {
                ScriptableObject.CreateInstance<ValueUsage>(),
                ScriptableObject.CreateInstance<ValueUsage>(),
            };
            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            var soUsage = new SerializedObject(usages);
            var serUsage = new SerializedScalableSettingValue(soUsage.FindProperty(nameof(ValueUsage.intValue)));

            // Same level values
            usages[0].intValue.useOverride = false;
            usages[0].intValue.level = 1;
            usages[1].intValue.useOverride = false;
            usages[1].intValue.level = 1;
            soUsage.SetIsDifferentCacheDirty();
            soUsage.Update();

            Assert.False(serUsage.hasMultipleValues);
            Assert.True(serUsage.TryGetValue(serDecl, out int v));
            Assert.AreEqual(2, v);

            Assert.True(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(2, v);

            // Same override value
            usages[0].intValue.useOverride = true;
            usages[0].intValue.@override = 5;
            usages[0].intValue.level = 1;
            usages[1].intValue.useOverride = true;
            usages[1].intValue.@override = 5;
            // Explicitly have a different level here
            // It must not matter
            usages[1].intValue.level = 2;
            soUsage.SetIsDifferentCacheDirty();
            soUsage.Update();

            Assert.False(serUsage.hasMultipleValues);
            Assert.True(serUsage.TryGetValue(serDecl, out v));
            Assert.AreEqual(5, v);

            Assert.True(serUsage.TryGetValue(decl.intValue, out v));
            Assert.AreEqual(5, v);
        }
    }
}
