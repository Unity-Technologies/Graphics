using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FilterModeEnum = UnityEditor.ShaderFoundry.SamplerStateAttribute.FilterModeEnum;
using WrapModeParameterStates = UnityEditor.ShaderFoundry.SamplerStateAttribute.WrapModeParameterStates;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UnitySamplerStatePropertyTests : BlockTestRenderer
    {
        SamplerStatePropertyBlockBuilder BuildWithoutNameOverrides(string fieldName)
        {
            var propBuilder = new SamplerStatePropertyBlockBuilder
            {
                FieldName = fieldName,
                PropertyAttribute = new PropertyAttributeData(),
            };
            return propBuilder;
        }

        SamplerStatePropertyBlockBuilder BuildWithNameOverrides(string fieldName, string uniformName)
        {
            var propBuilder = new SamplerStatePropertyBlockBuilder
            {
                FieldName = fieldName,
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = uniformName,
                },
            };
            return propBuilder;
        }

        [TestCase("MySampler", "_SamplerState_Linear_Repeat")]
        public void SamplerState_NoOverrides_NameIsExpected(string fieldName, string expectedUniformName)
        {
            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides(fieldName);
            var block = propBuilder.Build(container);
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase("MySampler", "_MySampler", "_SamplerState_Linear_Repeat")]
        public void SamplerState_UniformNameOverride_NameIsExpected(string fieldName, string uniformName, string expectedUniformName)
        {
            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides(fieldName, uniformName);
            var block = propBuilder.Build(container);
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase(null, "_Linear_Repeat")]
        [TestCase(FilterModeEnum.Point, "_Point_Repeat")]
        [TestCase(FilterModeEnum.Linear, "_Linear_Repeat")]
        [TestCase(FilterModeEnum.Trilinear, "_Trilinear_Repeat")]
        public void SamplerState_ValidateFilterMode_UniformIsExpectedName(FilterModeEnum? filterMode, string postfix)
        {
            var container = CreateContainer();

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.FilterMode = filterMode;

            var block = propBuilder.Build(container);
            var expectedUniformName = $"{propBuilder.GetUniformName()}{postfix}";
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase(null, "_Linear_Repeat")]
        [TestCase(WrapModeParameterStates.Repeat, "_Linear_Repeat")]
        [TestCase(WrapModeParameterStates.RepeatU, "_Linear_Repeat")]
        [TestCase(WrapModeParameterStates.RepeatV, "_Linear_Repeat")]
        [TestCase(WrapModeParameterStates.RepeatW, "_Linear_Repeat")]
        [TestCase(WrapModeParameterStates.Clamp, "_Linear_Clamp")]
        [TestCase(WrapModeParameterStates.ClampU, "_Linear_Repeat_ClampU")]
        [TestCase(WrapModeParameterStates.ClampV, "_Linear_Repeat_ClampV")]
        [TestCase(WrapModeParameterStates.ClampW, "_Linear_Repeat_ClampW")]
        [TestCase(WrapModeParameterStates.Mirror, "_Linear_Mirror")]
        [TestCase(WrapModeParameterStates.MirrorU, "_Linear_Repeat_MirrorU")]
        [TestCase(WrapModeParameterStates.MirrorV, "_Linear_Repeat_MirrorV")]
        [TestCase(WrapModeParameterStates.MirrorW, "_Linear_Repeat_MirrorW")]
        [TestCase(WrapModeParameterStates.MirrorOnce, "_Linear_MirrorOnce")]
        [TestCase(WrapModeParameterStates.MirrorOnceU, "_Linear_Repeat_MirrorOnceU")]
        [TestCase(WrapModeParameterStates.MirrorOnceV, "_Linear_Repeat_MirrorOnceV")]
        [TestCase(WrapModeParameterStates.MirrorOnceW, "_Linear_Repeat_MirrorOnceW")]
        public void SamplerState_ValidateSingleWrapMode_UniformIsExpectedName(WrapModeParameterStates? wrapMode, string postfix)
        {
            var wrapModes = new List<WrapModeParameterStates>();
            if (wrapMode.HasValue)
                wrapModes.Add(wrapMode.Value);
            SamplerState_ValidateWrapModes(wrapModes, postfix);
        }

        // Check overriding some channels with some being the default value (repeat)
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.MirrorV, WrapModeParameterStates.MirrorOnceW}, "_Linear_ClampU_MirrorV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.MirrorV, WrapModeParameterStates.MirrorOnceW}, "_Linear_RepeatU_MirrorV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.MirrorOnceW}, "_Linear_ClampU_RepeatV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.MirrorV}, "_Linear_ClampU_MirrorV_RepeatW")]
        // Check setting full channel default then overriding some channels
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.Clamp, WrapModeParameterStates.RepeatU, WrapModeParameterStates.MirrorV, WrapModeParameterStates.MirrorOnceW }, "_Linear_RepeatU_MirrorV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.Clamp, WrapModeParameterStates.MirrorV, WrapModeParameterStates.MirrorOnceW }, "_Linear_ClampU_MirrorV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.Clamp, WrapModeParameterStates.RepeatU, WrapModeParameterStates.MirrorOnceW }, "_Linear_RepeatU_ClampV_MirrorOnceW")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.Clamp, WrapModeParameterStates.RepeatU, WrapModeParameterStates.MirrorV }, "_Linear_RepeatU_MirrorV_ClampW")]
        // Check two states being equal and one being different
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.MirrorU, WrapModeParameterStates.ClampV, WrapModeParameterStates.ClampW }, "_Linear_Clamp_MirrorU")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.MirrorV, WrapModeParameterStates.ClampW }, "_Linear_Clamp_MirrorV")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.ClampV, WrapModeParameterStates.MirrorW }, "_Linear_Clamp_MirrorW")]
        // Check all three states being the same
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.ClampV, WrapModeParameterStates.ClampW }, "_Linear_Clamp")]
        // Check setting a state twice (last wins)
        // Setting a full-channel value doesn't wipe out a single channel value, even if specified afterwards
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.Mirror}, "_Linear_Mirror_ClampU")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampV, WrapModeParameterStates.Mirror}, "_Linear_Mirror_ClampV")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampW, WrapModeParameterStates.Mirror}, "_Linear_Mirror_ClampW")]
        // Different parameter order doesn't affect declaration order
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.MirrorOnceW, WrapModeParameterStates.ClampU, WrapModeParameterStates.MirrorV}, "_Linear_ClampU_MirrorV_MirrorOnceW")]
        public void SamplerState_ValidateMultipleWrapModes_UniformIsExpectedName(WrapModeParameterStates[] wrapModes, string postfix)
        {
            SamplerState_ValidateWrapModes(wrapModes.ToList(), postfix);
        }

        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.ClampU, WrapModeParameterStates.RepeatU }, "_Linear_Repeat", "Wrap mode 'RepeatU' will override state 'Clamp'.")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.RepeatV, WrapModeParameterStates.MirrorV }, "_Linear_Repeat_MirrorV", "Wrap mode 'MirrorV' will override state 'Repeat'.")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.MirrorW, WrapModeParameterStates.MirrorOnceW }, "_Linear_Repeat_MirrorOnceW", "Wrap mode 'MirrorOnceW' will override state 'Mirror'.")]
        [TestCase(new WrapModeParameterStates[] { WrapModeParameterStates.MirrorOnce, WrapModeParameterStates.Clamp }, "_Linear_Clamp", "Wrap mode 'Clamp' will override state 'MirrorOnce'.")]
        public void SamplerState_DuplicateWrapMode_WarningIsReported(WrapModeParameterStates[] wrapModes, string postfix, string expectedWarning)
        {
            LogAssert.Expect(LogType.Log, expectedWarning);
            // Make sure to still validate the name (verifying that the last value wins still)
            SamplerState_ValidateWrapModes(wrapModes.ToList(), postfix);
        }

        public void SamplerState_ValidateWrapModes(List<WrapModeParameterStates> wrapModes, string postfix)
        {
            var container = CreateContainer();

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.WrapModes = wrapModes;

            var block = propBuilder.Build(container);
            var expectedUniformName = $"{propBuilder.GetUniformName()}{postfix}";
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase(null, "_Linear_Repeat")]
        [TestCase(2, "_Linear_Repeat_aniso2")]
        [TestCase(4, "_Linear_Repeat_aniso4")]
        [TestCase(8, "_Linear_Repeat_aniso8")]
        [TestCase(16, "_Linear_Repeat_aniso16")]
        public void SamplerState_ValidateAnisotropicLevels_UniformIsExpectedName(int? anisotropicLevel, string postfix)
        {
            var container = CreateContainer();

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.AnisotropicLevel = anisotropicLevel;

            var block = propBuilder.Build(container);
            string expectedUniformName = $"{propBuilder.GetUniformName()}{postfix}";
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase(FilterModeEnum.Point, new[] {WrapModeParameterStates.Mirror }, true, 4, "_Point_Mirror_compare_aniso4")]
        public void SamplerState_FullParameters_UniformIsExpectedName(FilterModeEnum? filterMode, WrapModeParameterStates[] wrapModes, bool? depthCompare, int? anisotropicLevel, string postfix)
        {
            var container = CreateContainer();

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.FilterMode = filterMode;
            propBuilder.WrapModes = wrapModes.ToList();
            propBuilder.DepthCompare = depthCompare;
            propBuilder.AnisotropicLevel = anisotropicLevel;

            var block = propBuilder.Build(container);
            string expectedUniformName = $"{propBuilder.GetUniformName()}{postfix}";
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [TestCase(null, "_Linear_Repeat")]
        [TestCase(false, "_Linear_Repeat")]
        [TestCase(true, "_Linear_Repeat_compare")]
        public void SamplerState_DepthCompare_UniformIsExpectedName(bool? depthCompare, string postfix)
        {
            var container = CreateContainer();

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.DepthCompare = depthCompare;

            var block = propBuilder.Build(container);
            string expectedUniformName = $"{propBuilder.GetUniformName()}{postfix}";
            TestUniformName(container, block, propBuilder.BlockName, expectedUniformName);
        }

        [Test]
        public void MultipleSamplerStates_SettingsAreTheSame_NameIsDeduplicated()
        {
            var container = CreateContainer();
            var propBuilder1 = BuildWithoutNameOverrides("SamplerState1");
            propBuilder1.FilterMode = FilterModeEnum.Point;
            propBuilder1.AnisotropicLevel = 2;
            var block1 = propBuilder1.Build(container);

            var propBuilder2 = BuildWithoutNameOverrides("SamplerState2");
            propBuilder2.FilterMode = FilterModeEnum.Point;
            propBuilder2.AnisotropicLevel = 2;
            var block2 = propBuilder2.Build(container);

            var blocks = new List<Block> { block1, block2 };
            var shaderCode = BuildSimpleSurfaceShader(container, "SamplerStates", blocks);

            string expectedUniformName = $"{SamplerStateAttribute.BaseUniformName}_Point_Repeat_aniso2";
            TestUniformName(shaderCode, expectedUniformName);
        }

        [Test]
        public void MultipleSamplerStates_FilterModeIsDifferent_SeparateSamplerStatesDeclared()
        {
            var container = CreateContainer();
            var propBuilder1 = BuildWithoutNameOverrides("SamplerState1");
            propBuilder1.FilterMode = FilterModeEnum.Trilinear;
            var block1 = propBuilder1.Build(container);

            var propBuilder2 = BuildWithoutNameOverrides("SamplerState2");
            propBuilder2.FilterMode = FilterModeEnum.Point;
            var block2 = propBuilder2.Build(container);

            var blocks = new List<Block> { block1, block2 };
            var shaderCode = BuildSimpleSurfaceShader(container, "SamplerStates", blocks);

            string expectedUniformName1 = $"{SamplerStateAttribute.BaseUniformName}_Trilinear_Repeat";
            string expectedUniformName2 = $"{SamplerStateAttribute.BaseUniformName}_Point_Repeat";
            TestUniformName(shaderCode, expectedUniformName1);
            TestUniformName(shaderCode, expectedUniformName2);
        }

        [Test]
        public void MultipleSamplerStates_WrapModeIsDifferent_SeparateSamplerStatesDeclared()
        {
            var container = CreateContainer();
            var propBuilder1 = BuildWithoutNameOverrides("SamplerState1");
            propBuilder1.WrapModes = new List<WrapModeParameterStates> { WrapModeParameterStates.Clamp };
            var block1 = propBuilder1.Build(container);

            var propBuilder2 = BuildWithoutNameOverrides("SamplerState2");
            propBuilder2.WrapModes = new List<WrapModeParameterStates> { WrapModeParameterStates.Mirror };
            var block2 = propBuilder2.Build(container);

            var blocks = new List<Block> { block1, block2 };
            var shaderCode = BuildSimpleSurfaceShader(container, "SamplerStates", blocks);

            string expectedUniformName1 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Clamp";
            string expectedUniformName2 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Mirror";
            TestUniformName(shaderCode, expectedUniformName1);
            TestUniformName(shaderCode, expectedUniformName2);
        }

        [Test]
        public void MultipleSamplerStates_AnisotropicLevelIsDifferent_SeparateSamplerStatesDeclared()
        {
            var container = CreateContainer();
            var propBuilder1 = BuildWithoutNameOverrides("SamplerState1");
            propBuilder1.AnisotropicLevel = 2;
            var block1 = propBuilder1.Build(container);

            var propBuilder2 = BuildWithoutNameOverrides("SamplerState2");
            propBuilder2.AnisotropicLevel = 4;
            var block2 = propBuilder2.Build(container);

            var blocks = new List<Block> { block1, block2 };
            var shaderCode = BuildSimpleSurfaceShader(container, "SamplerStates", blocks);

            string expectedUniformName1 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Repeat_aniso2";
            string expectedUniformName2 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Repeat_aniso4";
            TestUniformName(shaderCode, expectedUniformName1);
            TestUniformName(shaderCode, expectedUniformName2);
        }

        [Test]
        public void MultipleSamplerStates_DepthCompareIsDifferent_SeparateSamplerStatesDeclared()
        {
            var container = CreateContainer();
            var propBuilder1 = BuildWithoutNameOverrides("SamplerState1");
            propBuilder1.DepthCompare = false;
            var block1 = propBuilder1.Build(container);

            var propBuilder2 = BuildWithoutNameOverrides("SamplerState2");
            propBuilder2.DepthCompare = true;
            var block2 = propBuilder2.Build(container);

            var blocks = new List<Block> { block1, block2 };
            var shaderCode = BuildSimpleSurfaceShader(container, "SamplerStates", blocks);

            string expectedUniformName1 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Repeat";
            string expectedUniformName2 = $"{SamplerStateAttribute.BaseUniformName}_Linear_Repeat_compare";
            TestUniformName(shaderCode, expectedUniformName1);
            TestUniformName(shaderCode, expectedUniformName2);
        }

        [Test]
        public void SamplerState_NoSamplerStateAttribute_ErrorIsReported()
        {
            string fieldName = "MySampler";
            var container = CreateContainer();
            var expectedExceptionMessage = $"{container._UnitySamplerState.Name} property '{fieldName}' must be declared with the '{SamplerStateAttribute.AttributeName}' attribute.";
            var shaderAttributes = new List<ShaderAttribute>() {};

            var propBuilder = BuildWithoutNameOverrides(fieldName);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase("1.2")]
        [TestCase("Foo")]
        public void SamplerState_InvalidFilterMode_ErrorIsReported(string filterMode)
        {
            var expectedExceptionMessage = $"Parameter {SamplerStateAttribute.FilterModeParamName} at index {0} with value {filterMode} must be a valid {typeof(SamplerStateAttribute.FilterModeEnum).Name} enum value.";
            var container = CreateContainer();

            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            attributeBuilder.Param(SamplerStateAttribute.FilterModeParamName, filterMode);
            var shaderAttributes = new List<ShaderAttribute>() { attributeBuilder.Build() };

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase("1.2", null)]
        [TestCase("Foo", null)]
        [TestCase("ClampZ", null)]
        // One value is correct but a second one is invalid
        [TestCase("ClampU,ClampQ", "ClampQ")]
        public void SamplerState_InvalidWrapMode_ErrorIsReported(string wrapMode, string invalidValueName)
        {
            invalidValueName = invalidValueName == null ? wrapMode : invalidValueName;
            var expectedExceptionMessage = $"Parameter {SamplerStateAttribute.WrapModeParamName} at index {0} with value {invalidValueName} must be a valid {typeof(SamplerStateAttribute.WrapModeParameterStates).Name} enum value.";
            var container = CreateContainer();

            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            attributeBuilder.Param(SamplerStateAttribute.WrapModeParamName, wrapMode);
            var shaderAttributes = new List<ShaderAttribute>() { attributeBuilder.Build() };

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase(1)]
        [TestCase(5)]
        public void SamplerState_InvalidAnisotropicLevelValue_ErrorIsReported(int anisotropicLevel)
        {
            var expectedExceptionMessage = $"Parameter {SamplerStateAttribute.AnisotropicLevelParamName} at position {0} with value {anisotropicLevel} must be an integer value of 2, 4, 8, or 16.";
            var container = CreateContainer();


            var propBuilder = BuildWithoutNameOverrides("MySampler");
            propBuilder.AnisotropicLevel = anisotropicLevel;

            var block = propBuilder.Build(container);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase("1.0")]
        [TestCase("SomeString")]
        public void SamplerState_InvalidAnisotropicLevelType_ErrorIsReported(string anisotropicLevel)
        {
            var expectedExceptionMessage = $"Parameter {SamplerStateAttribute.AnisotropicLevelParamName} at position {0} must be an integer.";
            var container = CreateContainer();

            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            attributeBuilder.Param(SamplerStateAttribute.AnisotropicLevelParamName, anisotropicLevel);
            var shaderAttributes = new List<ShaderAttribute>() { attributeBuilder.Build() };

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase("yes")]
        public void SamplerState_InvalidDepthCompareValue_ErrorIsReported(string depthCompareValue)
        {
            var expectedExceptionMessage = $"Parameter {SamplerStateAttribute.DepthCompareParamName} at position {0} must be a boolean.";
            var container = CreateContainer();

            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            attributeBuilder.Param(SamplerStateAttribute.DepthCompareParamName, depthCompareValue);
            var shaderAttributes = new List<ShaderAttribute>() { attributeBuilder.Build() };

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        [TestCase("MyParam", "MyValue")]
        public void SamplerState_InvalidAttributeParameter_ErrorIsReported(string paramName, string paramValue)
        {
            var expectedExceptionMessage = $"Unknown parameter {paramName} at position {0}.";
            var container = CreateContainer();

            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            attributeBuilder.Param(paramName, paramValue);
            var shaderAttributes = new List<ShaderAttribute>() { attributeBuilder.Build() };

            var propBuilder = BuildWithoutNameOverrides("MySampler");
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, block.Name, expectedExceptionMessage);
        }

        public void BuildAndExpectException(ShaderContainer container, Block block, string shaderName, string expectedExceptionMessage)
        {
            TestDelegate testDelegate = () => BuildSimpleSurfaceBlockShader(container, shaderName, block);
            var exception = Assert.Throws<System.Exception>(testDelegate);
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        static void TestUniformName(ShaderContainer container, Block block, string shaderName, string expectedUniformName)
        {
            var shaderCode = BuildSimpleSurfaceBlockShader(container, shaderName, block);
            TestUniformName(shaderCode, expectedUniformName);
        }

        static void TestUniformName(string shaderCode, string expectedUniformName)
        {
            // Check for the full uniform declaration. This also helps avoid partial string matches (e.g. Wrap matching to WrapU).
            var uniformDeclaration = $"SAMPLER({expectedUniformName})";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(uniformDeclaration), $"Expected to find uniform declaration {uniformDeclaration}");
        }
    }
}
