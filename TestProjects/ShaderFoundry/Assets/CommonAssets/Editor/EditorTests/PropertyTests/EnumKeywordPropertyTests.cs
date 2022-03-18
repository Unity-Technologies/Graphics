using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class EnumKeywordPropertyTests : BlockTestRenderer
    {
        EnumKeywordPropertyBlockBuilder BuildColorEnumKeywordBuilder(string defaultValue)
        {
            var propBuilder = new EnumKeywordPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Color",
                    DisplayName = "Color",
                    DefaultValue = defaultValue,
                },
                EnumEntries = new List<EnumKeywordPropertyBlockBuilder.EnumEntry>
                {
                    new EnumKeywordPropertyBlockBuilder.EnumEntry { Name = "Red", ResultValue = "float3(1, 0, 0)"},
                    new EnumKeywordPropertyBlockBuilder.EnumEntry { Name = "Green", ResultValue = "float3(0, 1, 0)"},
                    new EnumKeywordPropertyBlockBuilder.EnumEntry { Name = "Blue", ResultValue = "float3(0, 0, 1)"},
                }
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator EnumKeywordProperty_DefaultPropertyValueIsBlue_ColorIsBlue()
        {
            var expectedColor = new Color(0, 0, 1);

            var container = CreateContainer();
            var propBuilder = BuildColorEnumKeywordBuilder("2");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator EnumKeywordProperty_EnableKeywordRed_ColorIsRed()
        {
            var expectedColor = new Color(1, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildColorEnumKeywordBuilder("1");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m =>
            {
                // Have to disable the default value
                m.DisableKeyword(propBuilder.GetKeywordName("Green"));
                m.EnableKeyword(propBuilder.GetKeywordName("Red"));
            };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }

        [UnityTest]
        public IEnumerator EnumKeywordProperty_EnableKeywordGreen_ColorIsGreen()
        {
            var expectedColor = new Color(0, 1, 0);

            var container = CreateContainer();
            var propBuilder = BuildColorEnumKeywordBuilder("0");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m =>
            {
                // Have to disable the default value
                m.DisableKeyword(propBuilder.GetKeywordName("Red"));
                m.EnableKeyword(propBuilder.GetKeywordName("Green"));
            };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }

        [UnityTest]
        public IEnumerator EnumKeywordProperty_EnableKeywordBlue_ColorIsBlue()
        {
            var expectedColor = new Color(0, 0, 1);

            var container = CreateContainer();
            var propBuilder = BuildColorEnumKeywordBuilder("0");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m =>
            {
                // Have to disable the default value
                m.DisableKeyword(propBuilder.GetKeywordName("Red"));
                m.EnableKeyword(propBuilder.GetKeywordName("Blue"));
            };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
