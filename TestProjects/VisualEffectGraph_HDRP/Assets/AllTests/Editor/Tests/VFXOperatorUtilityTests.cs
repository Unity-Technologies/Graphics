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
    class VFXOperatorUtilityTests
    {
        [Test]
        public void ProcessOperatorBoxVolume()
        {
            var a = new Vector3(1.5f, 2.5f, 3.5f);
            var value_a = new VFXValue<Vector3>(a);
            var expressionA = VFXOperatorUtility.BoxVolume(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(a.x * a.y * a.z, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorCircleArea()
        {
            var a = 1.5f;
            var value_a = new VFXValue<float>(a);
            var expressionA = VFXOperatorUtility.CircleArea(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(a * a * Mathf.PI, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorCircleCircumference()
        {
            var a = 1.5f;
            var value_a = new VFXValue<float>(a);
            var expressionA = VFXOperatorUtility.CircleCircumference(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(a * 2.0f * Mathf.PI, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorClamp()
        {
            var a = -1.5f;
            var b = 0.2f;
            var c = 0.3f;
            var result = Mathf.Clamp(a, b, c);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var expression = VFXOperatorUtility.Clamp(value_a, value_b, value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessOperatorSaturate()
        {
            var a = -1.5f;
            var b = 0.2f;
            var c = 1.3f;
            var resultA = Mathf.Clamp(a, 0.0f, 1.0f);
            var resultB = Mathf.Clamp(b, 0.0f, 1.0f);
            var resultC = Mathf.Clamp(c, 0.0f, 1.0f);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var expressionA = VFXOperatorUtility.Saturate(value_a);
            var expressionB = VFXOperatorUtility.Saturate(value_b);
            var expressionC = VFXOperatorUtility.Saturate(value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);
            var resultExpressionC = context.Compile(expressionC);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>());
            Assert.AreEqual(resultB, resultExpressionB.Get<float>());
            Assert.AreEqual(resultC, resultExpressionC.Get<float>());
        }

        [Test]
        public void ProcessOperatorColorLuma()
        {
            Color a = new Color(0.2f, 0.5f, 0.3f);
            var result = (0.299f * a.r + 0.587f * a.g + 0.114f * a.b);

            var value_a = new VFXValue<Vector4>(a);

            var expression = VFXOperatorUtility.ColorLuma(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessOperatorConeVolume()
        {
            var a = 0.0f;
            var b = 1.5f;
            var c = 4.0f;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var expressionA = VFXOperatorUtility.ConeVolume(value_a, value_b, value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(9.424778f, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorCylinderVolume()
        {
            var a = 1.5f;
            var b = 4.0f;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expressionA = VFXOperatorUtility.CylinderVolume(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(28.274334f, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorDegToRad()
        {
            var a = -1.5f;
            var b = a * Mathf.Deg2Rad;

            var value_a = new VFXValue<float>(a);

            var expressionA = VFXOperatorUtility.DegToRad(value_a);
            var expressionB = VFXOperatorUtility.RadToDeg(expressionA);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual(b, resultExpressionA.Get<float>());
            Assert.AreEqual(a, resultExpressionB.Get<float>());
        }

        [Test]
        public void ProcessOperatorDiscretize()
        {
            var a = 1.5f;
            var b = 0.2f;
            var result = Mathf.Floor(a / b) * b;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expression = VFXOperatorUtility.Discretize(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorDistance()
        {
            var a = new Vector3(0.2f, 0.3f, 0.4f);
            var b = new Vector3(1.0f, 2.3f, 5.4f);
            var resultA = Vector3.Distance(a, b);
            var resultB = Vector3.Dot(a - b, a - b);

            var value_a = new VFXValue<Vector3>(a);
            var value_b = new VFXValue<Vector3>(b);

            var expressionA = VFXOperatorUtility.Distance(value_a, value_b);
            var expressionB = VFXOperatorUtility.SqrDistance(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>());
            Assert.AreEqual(resultB, resultExpressionB.Get<float>());
        }

        [Test]
        public void ProcessOperatorDot()
        {
            var a = new Vector2(0.2f, 0.3f);
            var b = new Vector2(1.0f, 2.3f);
            var c = new Vector3(0.2f, 0.3f, 0.4f);
            var d = new Vector3(1.0f, 2.3f, 5.4f);
            var e = new Vector4(0.2f, 0.3f, 0.4f, 4.0f);
            var f = new Vector4(1.0f, 2.3f, 5.4f, 0.6f);

            var resultA = Vector2.Dot(a, b);
            var resultB = Vector3.Dot(c, d);
            var resultC = Vector4.Dot(e, f);

            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector2>(b);
            var value_c = new VFXValue<Vector3>(c);
            var value_d = new VFXValue<Vector3>(d);
            var value_e = new VFXValue<Vector4>(e);
            var value_f = new VFXValue<Vector4>(f);

            var expressionA = VFXOperatorUtility.Dot(value_a, value_b);
            var expressionB = VFXOperatorUtility.Dot(value_c, value_d);
            var expressionC = VFXOperatorUtility.Dot(value_e, value_f);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);
            var resultExpressionC = context.Compile(expressionC);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>(), 0.0001f);
            Assert.AreEqual(resultB, resultExpressionB.Get<float>(), 0.0001f);
            Assert.AreEqual(resultC, resultExpressionC.Get<float>(), 0.0001f);
        }

        [Test]
        public void ProcessOperatorFit()
        {
            var value = 0.4f;
            var oldRangeMin = 0.2f;
            var oldRangeMax = 1.2f;
            var newRangeMin = 3.2f;
            var newRangeMax = 5.2f;

            var percent = (value - oldRangeMin) / (oldRangeMax - oldRangeMin);
            var result = Mathf.LerpUnclamped(newRangeMin, newRangeMax, percent);

            var value_a = new VFXValue<float>(value);
            var value_b = new VFXValue<float>(oldRangeMin);
            var value_c = new VFXValue<float>(oldRangeMax);
            var value_d = new VFXValue<float>(newRangeMin);
            var value_e = new VFXValue<float>(newRangeMax);

            var expression = VFXOperatorUtility.Fit(value_a, value_b, value_c, value_d, value_e);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessOperatorModuloFloat()
        {
            var a = -1.5f;
            var b = 0.2f;
            var ab = Mathf.Repeat(a, b);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expression = VFXOperatorUtility.Modulo(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(ab, resultExpression.Get<float>(), 0.001f);
        }

        #pragma warning disable 0414
        private static float[] ProcessOperatorAbs_a = new[] { -0.1f, 0.0f, 3.0f };

        #pragma warning restore 0414
        [Test]
        public void ProcessOperatorSign([ValueSource("ProcessOperatorAbs_a")] float a)
        {
            var r = Mathf.Sign(a);
            var value_a = new VFXValue<float>(a);

            var expression = new VFXExpressionSign(value_a);
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(r, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessOperatorAbs([ValueSource("ProcessOperatorAbs_a")] float a)
        {
            var r = Mathf.Abs(a);
            var value_a = new VFXValue<float>(a);

            var expression = new VFXExpressionAbs(value_a);
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(r, resultExpression.Get<float>());
        }

        #pragma warning disable 0414
        private static int[] ProcessOperatorAbsInt_a = new[] { -51, 0, 16787153 };

        #pragma warning restore 0414
        [Test]
        public void ProcessOperatorAbsInt([ValueSource("ProcessOperatorAbsInt_a")] int a)
        {
            var r = Mathf.Abs(a);
            var value_a = new VFXValue<int>(a);

            var expression = new VFXExpressionAbs(value_a);
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(r, resultExpression.Get<int>());
        }

        [Test]
        public void ProcessOperatorAbsUInt()
        {
            var value_a = new VFXValue<uint>(0u);
            Assert.Throws<NotImplementedException>(() => new VFXExpressionAbs(value_a));
        }

        [Test]
        public void ProcessOperatorSignUInt()
        {
            var value_a = new VFXValue<uint>(0u);
            Assert.Throws<NotImplementedException>(() => new VFXExpressionSign(value_a));
        }

        #pragma warning disable 0414
        private static int[] ProcessOperatorModuloInt_a = new[] { 78, 16777303 };
        private static int[] ProcessOperatorModuloInt_b = new[] { 7 };

        #pragma warning restore 0414
        [Test]
        public void ProcessOperatorModuloInt([ValueSource("ProcessOperatorModuloInt_a")] int a, [ValueSource("ProcessOperatorModuloInt_b")] int b)
        {
            var ab = a % b;

            var value_a = new VFXValue<int>(a);
            var value_b = new VFXValue<int>(b);

            var expression = VFXOperatorUtility.Modulo(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(ab, resultExpression.Get<int>());
        }

        [Test]
        public void ProcessOperatorFrac()
        {
            var a = -1.5f;
            var b = 0.2f;
            var resultA = a - Mathf.Floor(a);
            var resultB = b - Mathf.Floor(b);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expressionA = VFXOperatorUtility.Frac(value_a);
            var expressionB = VFXOperatorUtility.Frac(value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>());
            Assert.AreEqual(resultB, resultExpressionB.Get<float>());
        }

        [Test]
        public void ProcessOperatorLength()
        {
            var a = new Vector3(0.2f, 0.3f, 0.4f);
            var result = a.magnitude;

            var value_a = new VFXValue<Vector3>(a);

            var expression = VFXOperatorUtility.Length(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorLerp()
        {
            var a = new Vector3(0.2f, 0.3f, 0.4f);
            var b = new Vector3(1.0f, 2.3f, 5.4f);
            var c = 0.2f;
            var d = 1.5f;
            var resultA = Vector3.LerpUnclamped(a, b, c);
            var resultB = Vector3.LerpUnclamped(a, b, d);

            var value_a = new VFXValue<Vector3>(a);
            var value_b = new VFXValue<Vector3>(b);
            var value_c = new VFXValue<float>(c);
            var value_d = new VFXValue<float>(d);

            var expressionA = VFXOperatorUtility.Lerp(value_a, value_b, VFXOperatorUtility.CastFloat(value_c, value_b.valueType));
            var expressionB = VFXOperatorUtility.Lerp(value_a, value_b, VFXOperatorUtility.CastFloat(value_d, value_b.valueType));

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual((resultA - resultExpressionA.Get<Vector3>()).magnitude, 0.0f, 0.001f);
            Assert.AreEqual((resultB - resultExpressionB.Get<Vector3>()).magnitude, 0.0f, 0.001f);
        }

        [Test]
        public void ProcessOperatorNegate()
        {
            var a = -1.5f;
            var b = 0.2f;
            var resultA = -a;
            var resultB = -b;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expressionA = VFXOperatorUtility.Negate(value_a);
            var expressionB = VFXOperatorUtility.Negate(value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>());
            Assert.AreEqual(resultB, resultExpressionB.Get<float>());
        }

        [Test]
        public void ProcessOperatorNormalize()
        {
            var a = new Vector3(0.2f, 0.3f, 0.4f);
            var result = a.normalized;

            var value_a = new VFXValue<Vector3>(a);

            var expression = VFXOperatorUtility.Normalize(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(0.0f, (result - resultExpression.Get<Vector3>()).magnitude, 0.001f);
            Assert.AreEqual(1.0f, resultExpression.Get<Vector3>().magnitude, 0.001f);
        }

        [Test]
        public void ProcessOperatorPolarToRectangular()
        {
            var theta = 0.5f;
            var distance = 0.2f;

            var rectangular = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * distance;

            var value_theta = new VFXValue<float>(theta);
            var value_distance = new VFXValue<float>(distance);

            var expressionA = VFXOperatorUtility.PolarToRectangular(value_theta, value_distance);
            var expressionB = VFXOperatorUtility.RectangularToPolar(expressionA);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB0 = context.Compile(expressionB[0]);
            var resultExpressionB1 = context.Compile(expressionB[1]);

            Assert.AreEqual(rectangular, resultExpressionA.Get<Vector2>());
            Assert.AreEqual(theta, resultExpressionB0.Get<float>());
            Assert.AreEqual(distance, resultExpressionB1.Get<float>());
        }

        [Test]
        public void ProcessOperatorSphereVolume()
        {
            var a = 1.5f;
            var value_a = new VFXValue<float>(a);
            var expressionA = VFXOperatorUtility.SphereVolume(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(14.137167f, resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorSphericalToRectangular()
        {
            var theta = 0.2f;
            var phi = 0.4f;
            var distance = 0.5f;

            var rectangular = new Vector3(Mathf.Cos(phi) * Mathf.Cos(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(theta));
            rectangular *= distance;

            var value_theta = new VFXValue<float>(theta);
            var value_phi = new VFXValue<float>(phi);
            var value_distance = new VFXValue<float>(distance);

            var expressionA = VFXOperatorUtility.SphericalToRectangular(value_distance, value_theta, value_phi);
            var expressionB = VFXOperatorUtility.RectangularToSpherical(expressionA);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB0 = context.Compile(expressionB[0]);
            var resultExpressionB1 = context.Compile(expressionB[1]);
            var resultExpressionB2 = context.Compile(expressionB[2]);

            Assert.AreEqual(rectangular, resultExpressionA.Get<Vector3>());
            Assert.AreEqual(distance, resultExpressionB0.Get<float>(), 0.001f);
            Assert.AreEqual(theta, resultExpressionB1.Get<float>(), 0.001f);
            Assert.AreEqual(phi, resultExpressionB2.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorSmoothstep()
        {
            var b = 0.2f;
            var a = 1.2f;
            var c = 0.3f;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var expression = VFXOperatorUtility.Smoothstep(value_a, value_b, value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(0.971999943f, resultExpression.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorSqrt()
        {
            var a = 0.2f;
            var result = Mathf.Sqrt(a);

            var value_a = new VFXValue<float>(a);

            var expression = VFXOperatorUtility.Sqrt(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessOperatorTorusVolume()
        {
            var a = 4.0f;
            var b = 1.5f;

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expressionA = VFXOperatorUtility.TorusVolume(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(177.65288f, resultExpressionA.Get<float>(), 0.001f);
        }

        #pragma warning disable 0414
        static readonly float[] ProcessOperatorCeil_Values = { 4.0f, 1.5f, -0.5f};

        #pragma warning restore 0414
        [Test]
        public void ProcessOperatorCeil([ValueSource("ProcessOperatorCeil_Values")] float inValue)
        {
            var value = new VFXValue<float>(inValue);
            var expression = VFXOperatorUtility.Ceil(value);
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expression);
            Assert.AreEqual(Mathf.Ceil(inValue), resultExpressionA.Get<float>(), 0.001f);
        }

        [Test]
        public void ProcessOperatorCross()
        {
            var a = new Vector3(1.1f, 2.2f, 3.3f);
            var b = new Vector3(4.4f, 5.5f, 6.6f);

            var value_a = new VFXValue<Vector3>(a);
            var value_b = new VFXValue<Vector3>(b);

            var expressionA = VFXOperatorUtility.Cross(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);

            var resultExpressionA = context.Compile(expressionA);
            var resultValue = resultExpressionA.Get<Vector3>();

            var expectedValue = Vector3.Cross(a, b);

            Assert.AreEqual(expectedValue.x, resultValue.x, 0.001f);
            Assert.AreEqual(expectedValue.y, resultValue.y, 0.001f);
            Assert.AreEqual(expectedValue.z, resultValue.z, 0.001f);
        }

        static readonly string[] CheckAllBuiltinExpressionAreListed_ValueSource = VFXBuiltInExpression.All.Select(o => o.ToString()).ToArray();

        [Test]
        public void CheckBuiltinExpressionListed([ValueSource("CheckAllBuiltinExpressionAreListed_ValueSource")] string expressionName)
        {
            var operation = (UnityEngine.VFX.VFXExpressionOperation)Enum.Parse(typeof(UnityEngine.VFX.VFXExpressionOperation), expressionName);
            var referenceExpression = VFXBuiltInExpression.Find(operation);
            Assert.IsTrue(VFXDynamicBuiltInParameter.s_BuiltInInfo.Values.Any(o => o.expression == referenceExpression));
        }

        public struct ApplyAddressingModeTestCase
        {
            public ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode _mode, uint _count)
            {
                mode = _mode;
                count = _count;
                expectedSequence = new uint[Mathf.Max(50, 7 * (int)_count)];

                //Naive implementation for reference
                if (mode == VFXOperatorUtility.SequentialAddressingMode.Clamp)
                {
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = i < count ? i : count - 1;
                    }
                }
                else if (mode == VFXOperatorUtility.SequentialAddressingMode.Wrap)
                {
                    uint current = 0u;
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = current;
                        current++;
                        if (current >= count)
                            current = 0u;
                    }
                }
                else if (mode == VFXOperatorUtility.SequentialAddressingMode.Mirror)
                {
                    uint current = 0u;
                    bool increment = true;
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = current;
                        if (increment)
                        {
                            current++;
                            if (current >= count)
                            {
                                increment = false;
                                current = count > 2u ? count - 2u : 0u;
                            }
                        }
                        else
                        {
                            if (current == 0u)
                            {
                                increment = true;
                                current = count == 1u ? 0u : 1u;
                            }
                            else
                            {
                                current--;
                            }
                        }
                    }
                }
            }

            public VFXOperatorUtility.SequentialAddressingMode mode;
            public uint count;
            public uint[] expectedSequence;

            public override string ToString()
            {
                return string.Format("{0}_{1}_{2}", mode.ToString(), count, expectedSequence.Length);
            }
        }


        static readonly ApplyAddressingModeTestCase[] ApplyAddressingModeTestCase_ValueSource =
        {
            //The 0 case is always undefined
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Wrap, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Wrap, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Clamp, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Clamp, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 2u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 3u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 7u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 8u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 9u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 13u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 15u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 27u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 32u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 33u),
        };

        [Test]
        public void CheckExpectedSequence_ApplyAddressingMode([ValueSource("ApplyAddressingModeTestCase_ValueSource")] ApplyAddressingModeTestCase addressingMode)
        {
            var computedSequence = new uint[addressingMode.expectedSequence.Length];
            for (uint index = 0u; index < computedSequence.Length; ++index)
            {
                var indexExpr = VFXValue.Constant(index);
                var countExpr = VFXValue.Constant(addressingMode.count);
                var computed = VFXOperatorUtility.ApplyAddressingMode(indexExpr, countExpr, addressingMode.mode);

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(computed);

                computedSequence[index] = result.Get<uint>();
            }

            for (uint index = 0u; index < computedSequence.Length; ++index)
            {
                Assert.AreEqual(addressingMode.expectedSequence[index], computedSequence[index]);
            }
        }
    }
}
#endif
