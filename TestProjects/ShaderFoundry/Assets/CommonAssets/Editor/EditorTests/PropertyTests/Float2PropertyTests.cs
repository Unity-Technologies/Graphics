using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class Vector2PropertyTests : BlockTestRenderer
    {
        Vector2PropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new Vector2PropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "Float2Field",
            };
            return propBuilder;
        }

        Vector2PropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = BuildWithoutNameOverrides(defaultValue);
            propBuilder.PropertyAttribute.UniformName = "_Value";
            propBuilder.PropertyAttribute.DisplayName = "Value";
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator Float2Property_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 1, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("(1, 1, 0, 0)");
            var block = propBuilder.Build(container, container._float2);

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
            var block = propBuilder.Build(container, container._float2);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetVector(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }
    }
}
