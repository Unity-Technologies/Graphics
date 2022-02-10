using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class IntegerPropertyTests : BlockTestRenderer
    {
        IntegerPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new IntegerPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "IntField",
            };
            return propBuilder;
        }

        IntegerPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new IntegerPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = defaultValue
                },
                FieldName = "IntField",
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator IntegerProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 0, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("1");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator IntegerProperty_MaterialColorSet_IsExpectedColor()
        {
            var inputValue = 1;
            var expectedColor = new Color(inputValue, 0, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("0");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetInteger(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
