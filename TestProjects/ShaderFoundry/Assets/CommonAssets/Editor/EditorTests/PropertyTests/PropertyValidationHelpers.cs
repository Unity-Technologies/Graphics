using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal static class PropertyValidationHelpers
    {
        internal static void ValidateFloatProperty(Shader shader, int propertyIndex, BasePropertyBlockBuilder propBuilder, float expectedDefaultValue)
        {
            var uniformName = propBuilder.GetUniformName();
            var displayName = propBuilder.GetDisplayName();
            Assert.AreEqual(uniformName, shader.GetPropertyName(propertyIndex));
            Assert.AreEqual(displayName, shader.GetPropertyDescription(propertyIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Float, shader.GetPropertyType(propertyIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultFloatValue(propertyIndex));
        }

        internal static void ValidateIntegerProperty(Shader shader, int propertyIndex, BasePropertyBlockBuilder propBuilder, int expectedDefaultValue)
        {
            var uniformName = propBuilder.GetUniformName();
            var displayName = propBuilder.GetDisplayName();
            Assert.AreEqual(uniformName, shader.GetPropertyName(propertyIndex));
            Assert.AreEqual(displayName, shader.GetPropertyDescription(propertyIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Int, shader.GetPropertyType(propertyIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultIntValue(propertyIndex));
        }

        internal static void ValidateRangeProperty(Shader shader, int propertyIndex, BasePropertyBlockBuilder propBuilder, float expectedDefaultValue, Vector2 expectedRangeLimits)
        {
            var uniformName = propBuilder.GetUniformName();
            var displayName = propBuilder.GetDisplayName();
            Assert.AreEqual(uniformName, shader.GetPropertyName(propertyIndex));
            Assert.AreEqual(displayName, shader.GetPropertyDescription(propertyIndex));
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Range, shader.GetPropertyType(propertyIndex));
            Assert.AreEqual(expectedDefaultValue, shader.GetPropertyDefaultFloatValue(propertyIndex));
            Assert.AreEqual(expectedRangeLimits, shader.GetPropertyRangeLimits(propertyIndex));
        }
    }
}
