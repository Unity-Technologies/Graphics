using NUnit.Framework;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class SGVariableDeclarationModelTest : BaseGraphAssetTest
    {
        static readonly TypeHandle k_ExposableType = TypeHandle.Float;
        static readonly TypeHandle k_NonExposableType = ShaderGraphExampleTypes.SamplerStateTypeHandle;

        [Test]
        public void TestSetIsExposed_UpdatesContextEntry()
        {
            var variable = (SGVariableDeclarationModel)GraphModel.CreateGraphVariableDeclaration(k_ExposableType, "variable", ModifierFlags.None, false);

            variable.IsExposed = true;
            Assert.AreEqual(ContextEntryEnumTags.PropertyBlockUsage.Included, variable.ContextEntry
                .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                .GetData());

            variable.IsExposed = false;
            Assert.AreEqual(ContextEntryEnumTags.PropertyBlockUsage.Excluded, variable.ContextEntry
                .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                .GetData());
        }

        [Test]
        public void TestGetIsExposed_ExposableType_ReadsContextEntry()
        {
            var variable = (SGVariableDeclarationModel)GraphModel.CreateGraphVariableDeclaration(k_ExposableType, "variable", ModifierFlags.None, false);

            variable.ContextEntry
                .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                .SetData(ContextEntryEnumTags.PropertyBlockUsage.Excluded);
            Assert.IsFalse(variable.IsExposed);

            variable.ContextEntry
                .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                .SetData(ContextEntryEnumTags.PropertyBlockUsage.Included);
            Assert.IsTrue(variable.IsExposed);
        }

        [Test]
        public void TestGetIsExposed_NonExposableType_AlwaysFalse()
        {
            var variable = (SGVariableDeclarationModel)GraphModel.CreateGraphVariableDeclaration(k_NonExposableType, "variable", ModifierFlags.None, false);
            Assert.IsFalse(variable.IsExposed);

            variable.IsExposed = true;
            Assert.IsFalse(variable.IsExposed);
        }
    }
}
