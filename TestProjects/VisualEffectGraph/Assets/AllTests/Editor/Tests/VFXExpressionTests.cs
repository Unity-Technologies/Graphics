using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXExpressionTests
    {
        [Test]
        public void ProcessStoreValue()
        {
            var a = 123.0f;
            var b = 789.0f;

            var valueFloat = new VFXValue<float>(0.0f);
            valueFloat.SetContent(a);
            Assert.AreEqual(a, valueFloat.Get<float>());

            valueFloat.SetContent(new FloatN(b));
            Assert.AreEqual(b, valueFloat.Get<float>());
        }

        [Test]
        public void ProcessExpressionBasic()
        {
            //Reference some float math operation
            var a = new Vector2(0.75f, 0.5f);
            var b = new Vector3(1.3f, 0.2f, 0.7f);
            var c = 0.8f;
            var d = 0.1f;

            var refResultA = new Vector3(a.x + b.x, a.y + b.y, b.z);
            var refResultB = new Vector3(Mathf.Sin(refResultA.x), Mathf.Sin(refResultA.y), Mathf.Sin(refResultA.z));
            var refResultC = refResultB * c;
            var refResultD = new Vector3(d, d, d) - refResultC;

            //Using expression system
            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector3>(b);
            var value_c = new VFXValue<float>(c);
            var value_d = new VFXValue<float>(d);

            var addExpression = VFXOperatorUtility.CastFloat(value_a, value_b.valueType) + value_b;
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = (sinExpression * VFXOperatorUtility.CastFloat(value_c, sinExpression.valueType));
            var subtractExpression = VFXOperatorUtility.CastFloat(value_d, mulExpression.valueType) - mulExpression;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultA = context.Compile(addExpression);
            var resultB = context.Compile(sinExpression);
            var resultC = context.Compile(mulExpression);
            var resultD = context.Compile(subtractExpression);

            Assert.AreEqual(refResultA, resultA.Get<Vector3>());
            Assert.AreEqual(refResultB, resultB.Get<Vector3>());
            Assert.AreEqual(refResultC, resultC.Get<Vector3>());
            Assert.AreEqual(refResultD, resultD.Get<Vector3>());
        }

        [Test]
        public void ProcessExpressionSampleCurve()
        {
            var a = 0.564f;
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 0.7f), new Keyframe(0.8f, 0.1f), new Keyframe(1, 1));
            var resultRef = curve.Evaluate(a);

            var sampleValue = new VFXValue<float>(a);
            var curveValue = new VFXValue<AnimationCurve>(curve);
            var sampleCurve = new VFXExpressionSampleCurve(curveValue, sampleValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(sampleCurve);

            Assert.AreEqual(resultRef, reduced.Get<float>());
        }

        [Test]
        public void ProcessExpressionTestConcreteExpression()
        {
            var expressionTypes = typeof(VFXExpression)
                .Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(VFXExpression)) && !t.IsAbstract && !t.IsGenericType);

            var newInstanceHelper = typeof(VFXExpression).GetMethod("CreateNewInstance", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Type) }, null);
            Assert.AreNotEqual(newInstanceHelper, null);

            foreach (var expressionType in expressionTypes)
            {
                object newInstance = null;
                Assert.DoesNotThrow(() => { newInstance = newInstanceHelper.Invoke(null, new Type[] { expressionType }); },
                    "ProcessExpressionTestConcreteExpression fails with : " + expressionType.FullName);

                if (expressionType.GetConstructors().Any())
                {
                    Assert.AreNotEqual(newInstance, null);
                    Assert.AreEqual(newInstance.GetType(), expressionType);
                }
                else
                {
                    Assert.AreEqual(newInstance, null);
                }
            }
        }
    }
}
