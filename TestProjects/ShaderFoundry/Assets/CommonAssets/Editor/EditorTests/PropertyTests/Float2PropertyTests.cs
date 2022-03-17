using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class Float2PropertyTests : BlockTestRenderer
    {
        Float2PropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new Float2PropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "Float2Field",
            };
            return propBuilder;
        }

        Float2PropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new Float2PropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = defaultValue
                },
                FieldName = "Float2Field",
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator Float2Property_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 1, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("(1, 1, 0, 0)");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator Float2Property_MaterialColorSet_IsExpectedColor()
        {
            var inputValue = new Vector2(0.1f, 0.2f);
            var expectedColor = new Color(inputValue.x, inputValue.y, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("(1, 1, 0, 0)");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetVector(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
