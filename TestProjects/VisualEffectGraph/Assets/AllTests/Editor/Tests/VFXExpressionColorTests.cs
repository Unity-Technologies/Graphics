#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXExpressionColorTests
    {
        [Test]
        public void ProcessExpressionRGBtoHSV()
        {
            var rgb = new Vector3(0.5f, 0.2f, 0.8f);

            float h, s, v;
            Color color = new Color(rgb.x, rgb.y, rgb.z, 1.0f);
            Color.RGBToHSV(color, out h, out s, out v);
            var hsv = new Vector3(h, s, v);

            Color rgbResult = Color.HSVToRGB(hsv.x, hsv.y, hsv.z, true);
            var rgbAgain = new Vector3(rgbResult.r, rgbResult.g, rgbResult.b);

            var value_rgb = new VFXValue<Vector3>(rgb);
            var value_hsv = new VFXValue<Vector3>(hsv);

            var absExpressionA = new VFXExpressionRGBtoHSV(value_rgb);
            var absExpressionB = new VFXExpressionHSVtoRGB(value_hsv);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expressionA = context.Compile(absExpressionA);
            var expressionB = context.Compile(absExpressionB);

            Assert.AreEqual(hsv, expressionA.Get<Vector3>());
            Assert.AreEqual(rgb, expressionB.Get<Vector3>());
            Assert.AreEqual(rgb, rgbAgain);
        }
    }
}
#endif
