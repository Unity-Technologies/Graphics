using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    class VFXOperatorShapeMeasurementTests
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

            var expressionA = VFXOperatorUtility.ConeVolume(value_a, value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);

            Assert.AreEqual(28.274334f, resultExpressionA.Get<float>(), 0.001f);
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
    }
}
