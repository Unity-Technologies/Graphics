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
    }
}
#endif
