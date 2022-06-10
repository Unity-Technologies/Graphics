using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class IntegerPropertyTests : BlockTestRenderer
    {
        ScalarPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new ScalarPropertyBlockBuilder
            {
                BlockName = "IntegerProperty",
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "IntField",
            };
            return propBuilder;
        }

        ScalarPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = BuildWithoutNameOverrides(defaultValue);
            propBuilder.PropertyAttribute.UniformName = "_Value";
            propBuilder.PropertyAttribute.DisplayName = "Value";
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator IntegerProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 0, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("1");
            var block = propBuilder.Build(container, container._int);

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
            var block = propBuilder.Build(container, container._int);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetInteger(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }

        [Test]
        public void IntegerProperty_NoPropertyNameOverrides_ShaderPropertiesAreValid()
        {
            int expectedDefaultValue = 0;

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides(expectedDefaultValue.ToString());
            var block = propBuilder.Build(container, container._int);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);

            var propIndex = shader.FindPropertyIndex(propBuilder.FieldName);
            Assert.AreNotEqual(-1, propIndex);
            PropertyValidationHelpers.ValidateIntegerProperty(shader, propIndex, propBuilder, expectedDefaultValue);

            UnityEngine.Object.DestroyImmediate(shader);
        }

        [Test]
        public void IntegerProperty_RangeProperty_VerifyIsIntRange()
        {
            int expectedDefaultValue = 1;
            Vector2 expectedRangeLimits = new Vector2(0, 5);

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides(expectedDefaultValue.ToString());
            var rangeAttribute = new RangeAttribute() { Min = expectedRangeLimits.x, Max = expectedRangeLimits.y };
            var attributes = new List<ShaderAttribute> { rangeAttribute.Build(container) };
            var block = propBuilder.BuildWithAttributeOverrides(container, container._int, attributes);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);

            var propIndex = shader.FindPropertyIndex(propBuilder.PropertyAttribute.UniformName);
            Assert.AreNotEqual(-1, propIndex);
            PropertyValidationHelpers.ValidateRangeProperty(shader, propIndex, propBuilder, expectedDefaultValue, expectedRangeLimits);
            var propertyAttributes = shader.GetPropertyAttributes(propIndex);
            Assert.IsNotNull(propertyAttributes.FirstOrDefault((a) => (a == "IntRange")));

            UnityEngine.Object.DestroyImmediate(shader);
        }
    }
}
