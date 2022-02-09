using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class BoolKeywordPropertyTests : BlockTestRenderer
    {
        [UnityTest]
        public IEnumerator BooleanKeywordProperty_MaterialKeywordDefaultValueIsTrue_IsExpectedColor()
        {
            var expectedColor = new Color(1, 0, 0, 1);

            var container = CreateContainer();
            var propBuilder = new BoolKeywordPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = "1",
                },
                KeywordName = "_VALUE",
                TrueValue = "float3(1, 0, 0)",
                FalseValue = "float3(0, 0, 1)",
            };
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator BooleanKeywordProperty_MaterialKeywordDefaultValueIsFalse_IsExpectedColor()
        {
            var expectedColor = new Color(0, 0, 1, 1);

            var container = CreateContainer();
            var propBuilder = new BoolKeywordPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    DefaultValue = "0",
                },
                TrueValue = "float3(1, 0, 0)",
                FalseValue = "float3(0, 0, 1)",
            };
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator BooleanKeywordProperty_MaterialKeywordIsEnabled_IsExpectedColor()
        {
            var expectedColor = new Color(1, 0, 0, 1);

            var container = CreateContainer();
            var propBuilder = new BoolKeywordPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    DefaultValue = "1",
                },
                TrueValue = "float3(1, 0, 0)",
                FalseValue = "float3(0, 0, 1)",
            };
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate =  m => { m.EnableKeyword(propBuilder.KeywordName); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }

        [UnityTest]
        public IEnumerator BooleanKeywordProperty_MaterialKeywordIsDisabled_IsExpectedColor()
        {
            var expectedColor = new Color(0, 0, 1, 1);

            var container = CreateContainer();
            var propBuilder = new BoolKeywordPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    DefaultValue = "0",
                },
                TrueValue = "float3(1, 0, 0)",
                FalseValue = "float3(0, 0, 1)",
            };
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate =  m => { m.DisableKeyword(propBuilder.KeywordName); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
