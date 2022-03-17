using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class ColorPropertyTests : BlockTestRenderer
    {
        ColorPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new ColorPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "ColorField",
            };
            return propBuilder;
        }

        ColorPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new ColorPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_ColorValue",
                    DisplayName = "ColorValue",
                    DefaultValue = defaultValue
                },
                FieldName = "ColorField",
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator ColorProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("(0.1, 0.2, 0.3, 0.4)");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, errorThreshold: 1);
            yield break;
        }

        [UnityTest]
        public IEnumerator ColorProperty_MaterialColorSet_IsExpectedColor()
        {
            var expectedColor = new Color(0.1f, 0.2f, 0.3f, 1);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("(1, 1, 1, 1)");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetColor(propBuilder.PropertyAttribute.UniformName, expectedColor); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold: 1);
            yield break;
        }
    }
}
