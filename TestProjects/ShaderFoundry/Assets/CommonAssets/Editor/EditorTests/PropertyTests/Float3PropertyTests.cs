using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class Float3PropertyTests : BlockTestRenderer
    {
        Vector3PropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new Vector3PropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "Float3Field",
            };
            return propBuilder;
        }

        Vector3PropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = BuildWithoutNameOverrides(defaultValue);
            propBuilder.PropertyAttribute.UniformName = "_Value";
            propBuilder.PropertyAttribute.DisplayName = "Value";
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator Float3Property_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 1, 1, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("(1, 1, 1, 0)");
            var block = propBuilder.Build(container, container._float3);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator Float3Property_MaterialColorSet_IsExpectedColor()
        {
            var inputValue = new Vector3(0.1f, 0.2f, 0.3f);
            var expectedColor = new Color(inputValue.x, inputValue.y, inputValue.z, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("(1, 1, 1, 0)");
            var block = propBuilder.Build(container, container._float3);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetVector(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold: 1);
            yield break;
        }
    }
}
