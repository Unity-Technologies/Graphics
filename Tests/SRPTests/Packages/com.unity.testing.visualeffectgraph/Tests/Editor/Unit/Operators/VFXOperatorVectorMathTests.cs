using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    class VFXOperatorVectorMathTests
    {
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
    }
}
