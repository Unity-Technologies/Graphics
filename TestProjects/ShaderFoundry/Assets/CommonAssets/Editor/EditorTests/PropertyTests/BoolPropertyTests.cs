using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class BoolPropertyTests : BlockTestRenderer
    {
        BoolPropertyBlockBuilder BuildBoolWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new BoolPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
                FieldName = "BoolField",
            };
            return propBuilder;
        }

        BoolPropertyBlockBuilder BuildBoolWithNameOverrides(string defaultValue)
        {
            var propBuilder = new BoolPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = defaultValue
                },
                FieldName = "BoolField",
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator BoolProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(1, 1, 1, 1);
            var container = new ShaderFoundry.ShaderContainer();

            var propBuilder = BuildBoolWithNameOverrides("1");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolProperty_NoOverrides_IsExpectedColor()
        {
            var expectedColor = new Color(1, 1, 1, 1);
            var container = new ShaderFoundry.ShaderContainer();

            var propBuilder = BuildBoolWithoutNameOverrides("1");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolProperty_MaterialColorSet_IsExpectedColor()
        {
            var inputValue = 1.0f;
            var expectedColor = new Color(inputValue, inputValue, inputValue, 1);

            var container = CreateContainer();

            var propBuilder = BuildBoolWithNameOverrides("0");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetFloat(propBuilder.PropertyAttribute.UniformName, inputValue); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolProperty_NotExposed_ShaderPropertiesAreValid()
        {
            var inputValue = 1.0f;
            var expectedColor = new Color(inputValue, inputValue, inputValue, 1);

            var container = CreateContainer();

            var propBuilder = BuildBoolWithNameOverrides("0");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => {
                m.SetFloat(propBuilder.PropertyAttribute.UniformName, inputValue);
                var propIndex = m.shader.FindPropertyIndex(propBuilder.FieldName);
                Assert.AreEqual(-1, propIndex);
            };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);

            yield break;
        }

        [Test]
        public void BoolProperty_NoPropertyNameOverrides_ShaderPropertiesAreValid()
        {
            var expectedDefaultValue = 0.0f;

            var container = CreateContainer();
            var propBuilder = BuildBoolWithoutNameOverrides("0");
            var block = propBuilder.Build(container);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);

            var propIndex = shader.FindPropertyIndex(propBuilder.FieldName);
            Assert.AreNotEqual(-1, propIndex);
            Assert.AreEqual(propBuilder.FieldName, shader.GetPropertyName(propIndex));
            Assert.AreEqual(propBuilder.FieldName, shader.GetPropertyDescription(propIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultFloatValue(propIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Float, shader.GetPropertyType(propIndex));

            UnityEngine.Object.DestroyImmediate(shader);
        }

        [Test]
        public void BoolProperty_UniformAndDisplayNameOverridden_VerifyShaderProperty()
        {
            var expectedDefaultValue = 0.0f;

            var container = CreateContainer();
            var propBuilder = BuildBoolWithNameOverrides("0");
            var block = propBuilder.Build(container);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);

            var propIndex = shader.FindPropertyIndex(propBuilder.PropertyAttribute.UniformName);
            Assert.AreNotEqual(-1, propIndex);
            Assert.AreEqual(propBuilder.PropertyAttribute.UniformName, shader.GetPropertyName(propIndex));
            Assert.AreEqual(propBuilder.PropertyAttribute.DisplayName, shader.GetPropertyDescription(propIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultFloatValue(propIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Float, shader.GetPropertyType(propIndex));

            UnityEngine.Object.DestroyImmediate(shader);
        }

        [Test]
        public void BoolProperty_PropertyNoUniform_VerifyShaderProperty()
        {
            var expectedDefaultValue = 0.0f;

            var container = CreateContainer();
            var propBuilder = BuildBoolWithNameOverrides("0");
            propBuilder.PropertyAttribute.DataSource = ShaderFoundry.UniformDataSource.None;
            var block = propBuilder.Build(container);

            // This ideally needs to check that there's no uniform...
            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);

            var propIndex = shader.FindPropertyIndex(propBuilder.PropertyAttribute.UniformName);
            Assert.AreNotEqual(-1, propIndex);
            Assert.AreEqual(propBuilder.PropertyAttribute.UniformName, shader.GetPropertyName(propIndex));
            Assert.AreEqual(propBuilder.PropertyAttribute.DisplayName, shader.GetPropertyDescription(propIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultFloatValue(propIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Float, shader.GetPropertyType(propIndex));

            UnityEngine.Object.DestroyImmediate(shader);
        }
    }
}
