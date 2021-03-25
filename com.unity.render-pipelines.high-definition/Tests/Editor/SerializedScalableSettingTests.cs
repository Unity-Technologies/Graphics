using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class SerializedScalableSettingTests
    {
        class ValueDeclaration : ScriptableObject
        {
            public IntScalableSetting intValue = new IntScalableSetting(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);
        }

        [Test]
        public void TryGetLevelValueWorks()
        {
            var decl = ScriptableObject.CreateInstance<ValueDeclaration>();
            var soDecl = new SerializedObject(decl);
            var serDecl = new SerializedScalableSetting(soDecl.FindProperty(nameof(ValueDeclaration.intValue)));

            Assert.True(serDecl.TryGetLevelValue(0, out int v));
            Assert.AreEqual(1, v);

            Assert.True(serDecl.TryGetLevelValue(1, out v));
            Assert.AreEqual(2, v);

            Assert.True(serDecl.TryGetLevelValue(2, out v));
            Assert.AreEqual(3, v);

            Assert.False(serDecl.TryGetLevelValue(3, out v));
            Assert.AreEqual(default(int), v);
        }
    }
}
