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
    class VFXExpressionMathTests
    {
        [Test]
        public void ProcessExpressionAbs()
        {
            var a = -1.5f;
            var b = 0.0f;
            var c = 0.2f;
            var resultA = Mathf.Abs(a);
            var resultB = Mathf.Abs(b);
            var resultC = Mathf.Abs(c);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var absExpressionA = new VFXExpressionAbs(value_a);
            var absExpressionB = new VFXExpressionAbs(value_b);
            var absExpressionC = new VFXExpressionAbs(value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expressionA = context.Compile(absExpressionA);
            var expressionB = context.Compile(absExpressionB);
            var expressionC = context.Compile(absExpressionC);

            Assert.AreEqual(resultA, expressionA.Get<float>());
            Assert.AreEqual(resultB, expressionB.Get<float>());
            Assert.AreEqual(resultC, expressionC.Get<float>());
        }

        [Test]
        public void ProcessExpressionAdd()
        {
            var a = new Vector2(1.5f, 2.0f);
            var b = new Vector2(1.3f, 0.2f);
            var result = a + b;

            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector2>(b);

            var expression = (value_a + value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector2>());
        }

        [Test]
        public void ProcessExpressionBitwise()
        {
            int a = 12345;
            int b = 2;
            var resultA = a << b;
            var resultB = a >> b;
            var resultC = a | b;
            var resultD = a & b;
            var resultE = a ^ b;
            var resultF = ~a;

            var value_a = new VFXValue<uint>((uint)a);
            var value_b = new VFXValue<uint>((uint)b);

            var expressionA = new VFXExpressionBitwiseLeftShift(value_a, value_b);
            var expressionB = new VFXExpressionBitwiseRightShift(value_a, value_b);
            var expressionC = new VFXExpressionBitwiseOr(value_a, value_b);
            var expressionD = new VFXExpressionBitwiseAnd(value_a, value_b);
            var expressionE = new VFXExpressionBitwiseXor(value_a, value_b);
            var expressionF = new VFXExpressionBitwiseComplement(value_a);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);
            var resultExpressionC = context.Compile(expressionC);
            var resultExpressionD = context.Compile(expressionD);
            var resultExpressionE = context.Compile(expressionE);
            var resultExpressionF = context.Compile(expressionF);

            Assert.AreEqual((uint)resultA, resultExpressionA.Get<uint>());
            Assert.AreEqual((uint)resultB, resultExpressionB.Get<uint>());
            Assert.AreEqual((uint)resultC, resultExpressionC.Get<uint>());
            Assert.AreEqual((uint)resultD, resultExpressionD.Get<uint>());
            Assert.AreEqual((uint)resultE, resultExpressionE.Get<uint>());
            Assert.AreEqual((uint)resultF, resultExpressionF.Get<uint>());
        }

        [Test]
        public void ProcessExpressionDivide()
        {
            var a = new Vector2(1.5f, 2.0f);
            var b = new Vector2(1.3f, 0.2f);
            var result = new Vector2(a.x / b.x, a.y / b.y);

            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector2>(b);

            var expression = (value_a / value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector2>());
        }

        [Test]
        public void ProcessExpressionFloor()
        {
            var a = -1.5f;
            var b = 0.0f;
            var c = 0.2f;
            var resultA = Mathf.Floor(a);
            var resultB = Mathf.Floor(b);
            var resultC = Mathf.Floor(c);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var floorExpressionA = new VFXExpressionFloor(value_a);
            var floorExpressionB = new VFXExpressionFloor(value_b);
            var floorExpressionC = new VFXExpressionFloor(value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expressionA = context.Compile(floorExpressionA);
            var expressionB = context.Compile(floorExpressionB);
            var expressionC = context.Compile(floorExpressionC);

            Assert.AreEqual(resultA, expressionA.Get<float>());
            Assert.AreEqual(resultB, expressionB.Get<float>());
            Assert.AreEqual(resultC, expressionC.Get<float>());
        }

        [Test]
        public void ProcessExpressionLogical()
        {
            bool a = true;
            bool b = false;
            var resultA = a && b;
            var resultB = a || b;
            var resultC = !a;
            var resultD = !b;

            var value_a = new VFXValue<bool>((bool)a);
            var value_b = new VFXValue<bool>((bool)b);

            var expressionA = new VFXExpressionLogicalAnd(value_a, value_b);
            var expressionB = new VFXExpressionLogicalOr(value_a, value_b);
            var expressionC = new VFXExpressionLogicalNot(value_a);
            var expressionD = new VFXExpressionLogicalNot(value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);
            var resultExpressionC = context.Compile(expressionC);
            var resultExpressionD = context.Compile(expressionD);

            Assert.AreEqual((bool)resultA, resultExpressionA.Get<bool>());
            Assert.AreEqual((bool)resultB, resultExpressionB.Get<bool>());
            Assert.AreEqual((bool)resultC, resultExpressionC.Get<bool>());
            Assert.AreEqual((bool)resultD, resultExpressionD.Get<bool>());
        }

        [Test]
        public void ProcessExpressionMinMax()
        {
            var a = -1.5f;
            var b = 0.2f;
            var resultA = Mathf.Min(a, b);
            var resultB = Mathf.Max(a, b);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expressionA = new VFXExpressionMin(value_a, value_b);
            var expressionB = new VFXExpressionMax(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpressionA = context.Compile(expressionA);
            var resultExpressionB = context.Compile(expressionB);

            Assert.AreEqual(resultA, resultExpressionA.Get<float>());
            Assert.AreEqual(resultB, resultExpressionB.Get<float>());
            Assert.Greater(resultExpressionB.Get<float>(), resultExpressionA.Get<float>());
        }

        [Test]
        public void ProcessExpressionMul()
        {
            var a = new Vector2(1.5f, 2.0f);
            var b = new Vector2(1.3f, 0.2f);
            var result = Vector2.Scale(a, b);

            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector2>(b);

            var expression = (value_a * value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector2>());
        }

        [Test]
        public void ProcessExpressionPow()
        {
            var a = -1.5f;
            var b = 0.2f;
            var result = Mathf.Pow(a, b);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var expression = new VFXExpressionPow(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<float>());
        }

        [Test]
        public void ProcessExpressionSign()
        {
            var a = -1.5f;
            var b = 0.0f;
            var c = 0.2f;
            var resultA = Mathf.Sign(a);
            var resultB = Mathf.Sign(b);
            var resultC = Mathf.Sign(c);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);
            var value_c = new VFXValue<float>(c);

            var absExpressionA = new VFXExpressionSign(value_a);
            var absExpressionB = new VFXExpressionSign(value_b);
            var absExpressionC = new VFXExpressionSign(value_c);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expressionA = context.Compile(absExpressionA);
            var expressionB = context.Compile(absExpressionB);
            var expressionC = context.Compile(absExpressionC);

            Assert.AreEqual(resultA, expressionA.Get<float>());
            Assert.AreEqual(resultB, expressionB.Get<float>());
            Assert.AreEqual(resultC, expressionC.Get<float>());
        }

        [Test]
        public void ProcessExpressionSubtract()
        {
            var a = new Vector2(1.5f, 2.0f);
            var b = new Vector2(1.3f, 0.2f);
            var result = a - b;

            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector2>(b);

            var expression = (value_a - value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultExpression = context.Compile(expression);

            Assert.AreEqual(result, resultExpression.Get<Vector2>());
        }

        [Test]
        public void ProcessExpressionTrig()
        {
            var a = 1.5f;
            var b = 2.1f;
            var resultA = Mathf.Cos(a);
            var resultB = Mathf.Sin(a);
            var resultC = Mathf.Tan(a);
            var resultD = Mathf.Acos(a);
            var resultE = Mathf.Asin(a);
            var resultF = Mathf.Atan(a);
            var resultG = Mathf.Atan2(a, b);

            var value_a = new VFXValue<float>(a);
            var value_b = new VFXValue<float>(b);

            var cosExpression = new VFXExpressionCos(value_a);
            var sinExpression = new VFXExpressionSin(value_a);
            var tanExpression = new VFXExpressionTan(value_a);
            var acosExpression = new VFXExpressionACos(value_a);
            var asinExpression = new VFXExpressionASin(value_a);
            var atanExpression = new VFXExpressionATan(value_a);
            var atan2Expression = new VFXExpressionATan2(value_a, value_b);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var expressionA = context.Compile(cosExpression);
            var expressionB = context.Compile(sinExpression);
            var expressionC = context.Compile(tanExpression);
            var expressionD = context.Compile(acosExpression);
            var expressionE = context.Compile(asinExpression);
            var expressionF = context.Compile(atanExpression);
            var expressionG = context.Compile(atan2Expression);

            Assert.AreEqual(resultA, expressionA.Get<float>());
            Assert.AreEqual(resultB, expressionB.Get<float>());
            Assert.AreEqual(resultC, expressionC.Get<float>());
            Assert.AreEqual(resultD, expressionD.Get<float>());
            Assert.AreEqual(resultE, expressionE.Get<float>());
            Assert.AreEqual(resultF, expressionF.Get<float>());
            Assert.AreEqual(resultG, expressionG.Get<float>());
        }

        [Test]
        public void ProcessVanDerCorputSequence()
        {
            var expectedSequence = new[] { 0.0f, 1.0f / 2.0f, 1.0f / 4.0f, 3.0f / 4.0f, 1.0 / 8.0f, 5.0f / 8.0f, 3.0f / 8.0f, 7.0f / 8.0f, 1.0f / 16.0f, 9.0f / 16.0f, 5.0f / 16.0f, 13.0f / 16.0f, 3.0f / 16.0f, 11.0f / 16.0f, 7.0f / 16.0f, 15.0f / 16.0f };
            for (uint i = 0u; i < (uint)expectedSequence.Length; ++i)
            {
                var result = VFXOperatorUtility.VanDerCorputSequence(VFXValue.Constant(i));
                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var resultCompiled = context.Compile(result);
                var resultFloat = resultCompiled.Get<float>();
                Assert.AreEqual(expectedSequence[i], resultFloat);
            }
        }


        public struct Min_Max_Expression_Folding_TestCase
        {
            internal string name;
            internal VFXExpression expression;
            internal bool saturateExpected;

            public override string ToString()
            {
                return name;
            }
        };

        static private Min_Max_Expression_Folding_TestCase[] k_Min_Max_Expression_Folding_TestCase = Generate_Min_Max_Expression_Folding_TestCase().ToArray();
        static private string[] k_Min_Max_Expression_Folding_TestCase_Names = k_Min_Max_Expression_Folding_TestCase.Select(o => o.name).ToArray();

        static private IEnumerable<Min_Max_Expression_Folding_TestCase> Generate_Min_Max_Expression_Folding_TestCase()
        {
            var x = VFXBuiltInExpression.TotalTime;
            var one = VFXOperatorUtility.OneExpression[UnityEngine.Experimental.VFX.VFXValueType.Float];
            var zero = VFXOperatorUtility.ZeroExpression[UnityEngine.Experimental.VFX.VFXValueType.Float];

            //Good case
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(max(x, 0), 1)", expression = new VFXExpressionMin(new VFXExpressionMax(x, zero), one), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(1, (max(x, 0))", expression = new VFXExpressionMin(one, new VFXExpressionMax(x, zero)), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(min(x, 1), 0)", expression = new VFXExpressionMax(new VFXExpressionMin(x, one), zero), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(0, min(x, 1))", expression = new VFXExpressionMax(zero, new VFXExpressionMin(x, one)), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(max(0, x), 1)", expression = new VFXExpressionMin(new VFXExpressionMax(zero, x), one), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(1, (max(0, x))", expression = new VFXExpressionMin(one, new VFXExpressionMax(zero, x)), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(min(1, x), 0)", expression = new VFXExpressionMax(new VFXExpressionMin(one, x), zero), saturateExpected = true };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(0, min(1, x))", expression = new VFXExpressionMax(zero, new VFXExpressionMin(one, x)), saturateExpected = true };

            //bad case : Inverting 0 & 1
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(max(x, 1), 0)", expression = new VFXExpressionMin(new VFXExpressionMax(x, one), zero), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(0, (max(x, 1))", expression = new VFXExpressionMin(zero, new VFXExpressionMax(x, one)), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(min(x, 0), 1)", expression = new VFXExpressionMax(new VFXExpressionMin(x, zero), one), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(1, min(x, 0))", expression = new VFXExpressionMax(one, new VFXExpressionMin(x, zero)), saturateExpected = false };

            //Exotic cases 
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(min(x, 1), 0)", expression = new VFXExpressionMin(new VFXExpressionMin(x, one), zero), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(max(x, 1), 0)", expression = new VFXExpressionMax(new VFXExpressionMax(x, one), zero), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "max(add(x, 1), 0)", expression = new VFXExpressionMax(new VFXExpressionAdd(x, one), zero), saturateExpected = false };
            yield return new Min_Max_Expression_Folding_TestCase() { name = "min(1, (sub(x, 0))", expression = new VFXExpressionMin(one, new VFXExpressionSubtract(x, zero)), saturateExpected = false };
        }

        [Test]
        public void Min_Max_Expression_Folding([ValueSource("k_Min_Max_Expression_Folding_TestCase_Names")] string testCaseName)
        {
            var testCase = k_Min_Max_Expression_Folding_TestCase.First(o => o.name == testCaseName);
            var context = new VFXExpression.Context(VFXExpressionContextOption.Reduction);
            var resultCompiled = context.Compile(testCase.expression);

            if (testCase.saturateExpected)
            {
                Assert.IsTrue(resultCompiled is VFXExpressionSaturate);
            }
            else
            {
                Assert.IsFalse(resultCompiled is VFXExpressionSaturate);
            }
        }

        struct RoundExpression_TestCase
        {
            public float x;
            public float r;
            public string name { get { return string.Format("round({0}) = {1}", x, r); } }
        }

        static private IEnumerable<RoundExpression_TestCase> Generate_RoundExpression_TestCase()
        {
            yield return new RoundExpression_TestCase() { x = 0.0f, r = 0.0f };
            yield return new RoundExpression_TestCase() { x = 0.4999997f, r = 0.0f };
            yield return new RoundExpression_TestCase() { x = 0.5f, r = 0.0f }; //< Not really intuitive but fit with default HLSL behavior (nearbyintf)
            yield return new RoundExpression_TestCase() { x = 0.5000001f, r = 1.0f };
            yield return new RoundExpression_TestCase() { x = 1.5f, r = 2.0f };
            yield return new RoundExpression_TestCase() { x = 2.5f, r = 2.0f };
            yield return new RoundExpression_TestCase() { x = 3.5f, r = 4.0f };
            yield return new RoundExpression_TestCase() { x = 4.5f, r = 4.0f };
            yield return new RoundExpression_TestCase() { x = 6.5f, r = 6.0f };

            yield return new RoundExpression_TestCase() { x = -0.4999997f, r = 0.0f };
            yield return new RoundExpression_TestCase() { x = -0.5f, r = 0.0f };
            yield return new RoundExpression_TestCase() { x = -0.5000001f, r = -1.0f };
            yield return new RoundExpression_TestCase() { x = -1.5f, r = -2.0f };
            yield return new RoundExpression_TestCase() { x = -2.5f, r = -2.0f };
            yield return new RoundExpression_TestCase() { x = -3.5f, r = -4.0f };
            yield return new RoundExpression_TestCase() { x = -4.5f, r = -4.0f };
            yield return new RoundExpression_TestCase() { x = -6.5f, r = -6.0f };
        }

        static private RoundExpression_TestCase[] k_RoundExpression_TestCase = Generate_RoundExpression_TestCase().ToArray();
        static private string[] k_RoundExpression_TestCase_Names = k_RoundExpression_TestCase.Select(o => o.name).ToArray();

        [Test]
        public void Round_Expression([ValueSource("k_RoundExpression_TestCase_Names")] string testCaseName)
        {
            var testCase = k_RoundExpression_TestCase.First(o => o.name == testCaseName);
            var valueConstant = new VFXValue<float>(testCase.x);
            var round = new VFXExpressionRound(valueConstant);

            var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);
            var resultCompiled = context.Compile(round);
            Assert.IsTrue(resultCompiled is VFXValue);
            Assert.AreEqual(testCase.r, (resultCompiled as VFXValue).Get<float>());
        }
    }
}
#endif
