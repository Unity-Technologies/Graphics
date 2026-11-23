#if ENABLE_UIELEMENTS_MODULE && UNITY_ENABLE_CHECKS
#define ENABLE_RENDERING_DEBUGGER_UI
#endif
#if ENABLE_RENDERING_DEBUGGER_UI
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class DebugUITests
    {
        [Test]
        public void RoundToPrecision_WithZeroDecimalPlaces_RoundsToInteger()
        {
            // Reference value is an integer (0 decimal places)
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(1.567f, 1.0f);
            Assert.AreEqual(2.0f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithOneDecimalPlace_RoundsCorrectly()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(1.567f, 0.1f);
            Assert.AreEqual(1.6f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithTwoDecimalPlaces_RoundsCorrectly()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(1.567f, 0.01f);
            Assert.AreEqual(1.57f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithMultipleReferenceValues_UsesMaximumPrecision()
        {
            // Should use 3 decimal places from 0.001f (ignoring 0.1f and 1.0f)
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(1.5678f, 1.0f, 0.1f, 0.001f);
            Assert.AreEqual(1.568f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithNegativeValue_RoundsCorrectly()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(-1.567f, 0.01f);
            Assert.AreEqual(-1.57f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithZeroValue_ReturnsZero()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(0.0f, 0.01f);
            Assert.AreEqual(0.0f, result, 0.0001f);
        }

        [Test]
        public void RoundToPrecision_WithLargeValue_HandlesCorrectly()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(12345.678f, 0.01f);
            Assert.AreEqual(12345.68f, result, 0.01f);
        }

        [Test]
        public void RoundToPrecision_WithLargeValueAndRefValue_HandlesCorrectly()
        {
            float result = DebugUI.DebugUIStepperHelper.RoundToPrecision(12345.0f, 100.0f);
            Assert.AreEqual(12345.0f, result, 0.01f);
        }

        [TestCase(-5, ExpectedResult = 10)]
        [TestCase(15, ExpectedResult = 15)]
        [TestCase(25, ExpectedResult = 20)]
        public int IntField_ValidateValue_ClampsToMinMax(int value)
        {
            var field = new DebugUI.IntField { min = () => 10, max = () => 20 };
            return field.ValidateValue(value);
        }

        [TestCase(int.MinValue, ExpectedResult = int.MinValue)]
        [TestCase(int.MaxValue, ExpectedResult = int.MaxValue)]
        [TestCase(123456789, ExpectedResult = 123456789)]
        public int IntField_ValidateValue_PreservesValueWithinFullRange(int value)
        {
            var field = new DebugUI.IntField { min = () => int.MinValue, max = () => int.MaxValue };
            return field.ValidateValue(value);
        }

        [TestCase(5u, ExpectedResult = 10u)]
        [TestCase(15u, ExpectedResult = 15u)]
        [TestCase(25u, ExpectedResult = 20u)]
        public uint UIntField_ValidateValue_ClampsToMinMax(uint value)
        {
            var field = new DebugUI.UIntField { min = () => 10u, max = () => 20u };
            return field.ValidateValue(value);
        }

        [TestCase(0u, ExpectedResult = 0u)]
        [TestCase(uint.MaxValue, ExpectedResult = uint.MaxValue)]
        [TestCase(123456789u, ExpectedResult = 123456789u)]
        public uint UIntField_ValidateValue_PreservesValueWithinFullRange(uint value)
        {
            var field = new DebugUI.UIntField { min = () => 0u, max = () => uint.MaxValue };
            return field.ValidateValue(value);
        }

        [TestCase(-5f, ExpectedResult = 10f)]
        [TestCase(15.5f, ExpectedResult = 15.5f)]
        [TestCase(25f, ExpectedResult = 20f)]
        public float FloatField_ValidateValue_ClampsToMinMax(float value)
        {
            var field = new DebugUI.FloatField { min = () => 10f, max = () => 20f };
            return field.ValidateValue(value);
        }
    }
}
#endif
