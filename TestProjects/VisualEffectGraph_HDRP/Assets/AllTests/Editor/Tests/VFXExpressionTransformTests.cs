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
    class VFXExpressionTransformTests
    {
        [Test]
        public void ProcessExpressionTRSToMatrix()
        {
            var t = new Vector3(0.2f, 0.3f, 0.4f);
            var r = new Vector3(0.5f, 0.6f, 0.7f);
            var s = new Vector3(0.8f, 0.9f, 1.0f);

            var q = Quaternion.Euler(r);

            Matrix4x4 result = new Matrix4x4();
            result.SetTRS(t, q, s);

            var value_t = new VFXValue<Vector3>(t);
            var value_r = new VFXValue<Vector3>(r);
            var value_s = new VFXValue<Vector3>(s);

            var expressionA = new VFXExpressionTRSToMatrix(new VFXExpression[] { value_t, value_r, value_s });
            var expressionB = new VFXExpressionExtractPositionFromMatrix(expressionA);
            var expressionC = new VFXExpressionExtractAnglesFromMatrix(expressionA);
            var expressionD = new VFXExpressionExtractScaleFromMatrix(expressionA);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);
            var resultExpressionC = context.Compile(expressionC);
            var resultExpressionD = context.Compile(expressionD);

            Assert.AreEqual(result, resultExpressionA.Get<Matrix4x4>());
            Assert.AreEqual((t - resultExpressionB.Get<Vector3>()).magnitude, 0.0f, 0.01f);
            Assert.AreEqual((r - resultExpressionC.Get<Vector3>()).magnitude, 0.0f, 0.01f);
            Assert.AreEqual((s - resultExpressionD.Get<Vector3>()).magnitude, 0.0f, 0.01f);
        }

        [Test]
        public void ProcessExpressionInverseMatrix()
        {
            var t = new Vector3(0.2f, 0.3f, 0.4f);
            var r = new Vector3(0.5f, 0.6f, 0.7f);
            var s = new Vector3(0.8f, 0.9f, 1.0f);

            var q = Quaternion.Euler(r);

            Matrix4x4 inputMatrix = new Matrix4x4();
            inputMatrix.SetTRS(t, q, s);
            Matrix4x4 outputMatrix = inputMatrix.inverse;

            var matrixExpression = VFXValue.Constant(inputMatrix);
            var invMatrixExpressions = new VFXExpressionInverseMatrix(matrixExpression);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(invMatrixExpressions);

            Assert.AreEqual(resultExpression.Get<Matrix4x4>(), outputMatrix);
        }

        [Test]
        public void ProcessExpressionTransformPosition()
        {
            var t = new Vector3(0.2f, 0.3f, 0.4f);
            var m = new Matrix4x4();
            m.SetTRS(new Vector3(1.0f, 2.0f, 3.0f), Quaternion.Euler(0.2f, 0.3f, 0.4f), new Vector3(2.0f, 3.0f, 4.0f));

            var result = m.MultiplyPoint(t);

            var value_t = new VFXValue<Vector3>(t);
            var value_m = new VFXValue<Matrix4x4>(m);

            var expression = new VFXExpressionTransformPosition(value_m, value_t);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector3>());
        }

        [Test]
        public void ProcessExpressionTransformVector()
        {
            var v = new Vector3(0.2f, 0.3f, 0.4f);
            var m = new Matrix4x4();
            m.SetTRS(new Vector3(1.0f, 2.0f, 3.0f), Quaternion.Euler(0.2f, 0.3f, 0.4f), new Vector3(2.0f, 3.0f, 4.0f));

            var result = m.MultiplyVector(v);

            var value_v = new VFXValue<Vector3>(v);
            var value_m = new VFXValue<Matrix4x4>(m);

            var expression = new VFXExpressionTransformVector(value_m, value_v);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector3>());
        }

        [Test]
        public void ProcessExpressionTransformDirection()
        {
            var v = new Vector3(0.2f, 0.3f, 0.4f).normalized;
            var m = new Matrix4x4();
            m.SetTRS(new Vector3(1.0f, 2.0f, 3.0f), Quaternion.Euler(0.2f, 0.3f, 0.4f), new Vector3(2.0f, 3.0f, 4.0f));

            var result = m.MultiplyVector(v).normalized;

            var value_v = new VFXValue<Vector3>(v);
            var value_m = new VFXValue<Matrix4x4>(m);

            var expression = new VFXExpressionTransformDirection(value_m, value_v);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector3>());
            Assert.AreEqual(1.0f, resultExpression.Get<Vector3>().magnitude, 0.01f);
        }
    }
}
#endif
