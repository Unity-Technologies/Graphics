using NUnit.Framework;
using System;
using UnityEngine;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    class VFXOperatorScalarMathTests
    {
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
            var r = (a < 0.0f) ? -1.0f : (a > 0.0f) ? 1.0f : 0.0f;
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
    }
}
