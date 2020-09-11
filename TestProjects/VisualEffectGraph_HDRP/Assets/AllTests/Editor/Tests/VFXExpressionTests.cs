#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX;
using UnityEngine.VFX;

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

            valueFloat.SetContent(b);
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

        private static void TestConditions(float f0, float f1)
        {
            var f0Exp = VFXValue.Constant(f0);
            var f1Exp = VFXValue.Constant(f1);

            var equalExp = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Equal, f0Exp, f1Exp);
            var notEqualExp = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.NotEqual, f0Exp, f1Exp);
            var lessExp = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, f0Exp, f1Exp);
            var lessOrEqualExp = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.LessOrEqual, f0Exp, f1Exp);
            var greater = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater, f0Exp, f1Exp);
            var greaterOrEqual = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.GreaterOrEqual, f0Exp, f1Exp);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultA = context.Compile(equalExp);
            var resultB = context.Compile(notEqualExp);
            var resultC = context.Compile(lessExp);
            var resultD = context.Compile(lessOrEqualExp);
            var resultE = context.Compile(greater);
            var resultF = context.Compile(greaterOrEqual);

            Assert.AreEqual(f0 == f1, resultA.Get<bool>());
            Assert.AreEqual(f0 != f1, resultB.Get<bool>());
            Assert.AreEqual(f0 < f1, resultC.Get<bool>());
            Assert.AreEqual(f0 <= f1, resultD.Get<bool>());
            Assert.AreEqual(f0 > f1, resultE.Get<bool>());
            Assert.AreEqual(f0 >= f1, resultF.Get<bool>());
        }

        [Test]
        public void ProcessExpressionConditions()
        {
            TestConditions(0.0f, 0.0f);
            TestConditions(0.0f, 1.0f);
            TestConditions(1.0f, 0.0f);
        }

        [Test]
        public void ProcessExpressionBranch()
        {
            var branch0 = VFXValue.Constant(Vector3.right);
            var branch1 = VFXValue.Constant(Vector3.up);

            var test0 = new VFXExpressionBranch(VFXValue.Constant(true), branch0, branch1);
            var test1 = new VFXExpressionBranch(VFXValue.Constant(false), branch0, branch1);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultA = context.Compile(test0);
            var resultB = context.Compile(test1);

            Assert.AreEqual(Vector3.right, resultA.Get<Vector3>());
            Assert.AreEqual(Vector3.up, resultB.Get<Vector3>());

            // Test static branching
            context = new VFXExpression.Context(VFXExpressionContextOption.Reduction);
            var resultC = context.Compile(test0);
            Assert.AreEqual(branch0, resultC);
        }

        [Test]
        public void ProcessExpressionVector3sToMatrix()
        {
            var x = Vector3.right;
            var y = Vector3.up;
            var z = Vector3.forward;
            var w = Vector3.zero;

            var xValue = VFXValue.Constant(x);
            var yValue = VFXValue.Constant(y);
            var zValue = VFXValue.Constant(z);
            var wValue = VFXValue.Constant(w);

            var matrixValue = new VFXExpressionVector3sToMatrix(xValue, yValue, zValue, wValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(matrixValue);

            Assert.AreEqual(Matrix4x4.identity, reduced.Get<Matrix4x4>());
        }

        [Test]
        public void ProcessExpressionVector4sToMatrix()
        {
            var x = new Vector4(1, 0, 0, 0);
            var y = new Vector4(0, 1, 0, 0);
            var z = new Vector4(0, 0, 1, 0);
            var w = new Vector4(0, 0, 0, 1);

            var xValue = VFXValue.Constant(x);
            var yValue = VFXValue.Constant(y);
            var zValue = VFXValue.Constant(z);
            var wValue = VFXValue.Constant(w);

            var matrixValue = new VFXExpressionVector4sToMatrix(xValue, yValue, zValue, wValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(matrixValue);

            Assert.AreEqual(Matrix4x4.identity, reduced.Get<Matrix4x4>());
        }

        [Test]
        public void ProcessExpressionMatrixToVector3s()
        {
            var matValue = VFXValue.Constant(Matrix4x4.identity);
            var axisValue = VFXValue.Constant<int>(0);

            var xValue = new VFXExpressionMatrixToVector3s(matValue, axisValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(xValue);

            Assert.AreEqual(Vector3.right, reduced.Get<Vector3>());
        }

        [Test]
        public void ProcessExpressionMatrixToVector4s()
        {
            var matValue = VFXValue.Constant(Matrix4x4.identity);
            var axisValue = VFXValue.Constant<int>(0);

            var xValue = new VFXExpressionMatrixToVector4s(matValue, axisValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(xValue);

            Assert.AreEqual(new Vector4(1, 0, 0, 0), reduced.Get<Vector4>());
        }


        [Test]
        public void OuputExpression_From_Slot_Mesh_Should_Be_Invalid_Constant()
        {
            var source = ScriptableObject.CreateInstance<VFXInlineOperator>();
            source.SetSettingValue("m_Type", (SerializableType)typeof(Mesh));
            var expressionOutput = source.outputSlots[0].GetExpression();

            var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);
            var reduced = context.Compile(expressionOutput);

            Assert.IsTrue(expressionOutput.Is(VFXExpression.Flags.InvalidConstant));
        }

        [Test]
        public void OuputExpression_From_Slot_Mesh_Should_Be_Invalid_Constant_Propagation()
        {
            var source = ScriptableObject.CreateInstance<VFXInlineOperator>();
            source.SetSettingValue("m_Type", (SerializableType)typeof(Mesh));

            var meshCount = ScriptableObject.CreateInstance<Operator.MeshVertexCount>();
            meshCount.inputSlots[0].Link(source.outputSlots[0]);

            var add = ScriptableObject.CreateInstance<Operator.Add>();
            add.SetOperandType(0, typeof(uint));
            add.SetOperandType(1, typeof(uint));
            add.inputSlots[1].value = 8u;

            var expressionOutputBefore = add.outputSlots[0].GetExpression();
            var contextBefore = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding); //Used by runtime
            var reducedBeforeLink = contextBefore.Compile(expressionOutputBefore);

            bool success = add.inputSlots[0].Link(meshCount.outputSlots[0]);
            Assert.IsTrue(success);

            var expressionOutputAfter = add.outputSlots[0].GetExpression();
            var contextAfter = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding); //Used by runtime
            var reducedAfterLink = contextAfter.Compile(expressionOutputAfter);

            var contextAfterCPUEvaluation = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding); //Used by GUI
            var reducedAfterLinkCPUEvaluation = contextAfterCPUEvaluation.Compile(expressionOutputAfter);

            Assert.IsAssignableFrom(typeof(VFXValue<uint>), reducedBeforeLink);
            Assert.IsAssignableFrom(typeof(VFXExpressionAdd), reducedAfterLink);
            Assert.IsAssignableFrom(typeof(VFXValue<uint>), reducedAfterLinkCPUEvaluation);
        }

        [Test]
        public void CheckExpressionRandomEquality()
        {
            var obj0 = new object();
            var obj1 = new object();

            var exp0 = new VFXExpressionRandom(true, new RandId(obj0));
            var exp1 = new VFXExpressionRandom(true, new RandId(obj0));
            var exp2 = new VFXExpressionRandom(false, new RandId(obj0));
            var exp3 = new VFXExpressionRandom(true, new RandId(obj1));
            var exp4 = new VFXExpressionRandom(true, new RandId(obj0, 1));

            Assert.AreEqual(exp0, exp1);
            Assert.AreEqual(exp0.GetHashCode(), exp1.GetHashCode());

            Assert.AreNotEqual(exp0, exp2);
            Assert.AreNotEqual(exp0, exp3);
            Assert.AreNotEqual(exp0, exp4);
        }
    }
}
#endif
