using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class FloatPropertyTests : BlockTestRenderer
    {
        FloatPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new FloatPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "FloatField",
            };
            return propBuilder;
        }

        FloatPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new FloatPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = defaultValue
                },
                FieldName = "FloatField",
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator FloatProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 0, 0, 1);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("1");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator FloatProperty_MaterialColorSet_IsExpectedColor()
        {
            var inputValue = 0.1f;
            var expectedColor = new Color(inputValue, 0, 0, 1);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("1");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetFloat(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
