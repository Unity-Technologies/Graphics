using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    public class VFXOperatorNumericEvaluationTests
    {
        [Test]
        public void CascadedMulOperator_Mix_Vector2_and_Vector3()
        {
            var vec2_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var vec3_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();

            vec2_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector2));
            vec2_Two.inputSlots[0].value = Vector2.one * 2.0f;

            vec3_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            vec3_Two.inputSlots[0].value = Vector3.one * 2.0f;

            var mul = ScriptableObject.CreateInstance<Operator.Multiply>();
            mul.SetOperandType(0, typeof(Vector2));
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.SetOperandType(1, typeof(Vector3));
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(mul.outputSlots[0].GetExpression());
            var final = result.Get<Vector3>();

            Assert.AreEqual(new Vector3(4, 4, 2), final);
        }

        [Test]
        public void CascadedSubstractOperator_Mixing_Integer_With_OtherType()
        {
            var subtract = ScriptableObject.CreateInstance<Operator.Subtract>();
            subtract.SetOperandType(0, typeof(float));
            subtract.SetOperandType(1, typeof(float));

            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(float));
            subtract.SetOperandType(1, typeof(int));
            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(int));
            Assert.AreEqual(VFXValueType.Int32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(uint));
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, typeof(uint));
            subtract.SetOperandType(1, typeof(uint));
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            //Finally, do some simple math integer
            subtract.SetOperandType(0, typeof(int));
            subtract.SetOperandType(1, typeof(uint));

            var a = 9;
            var b = 5;
            var res = a - b; //remark : here, in C#, if b is an uint, result is a long not an uint

            subtract.inputSlots[0].value = (int)a;
            subtract.inputSlots[1].value = (uint)b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(subtract.outputSlots[0].GetExpression());
            var final = result.Get<uint>();
            Assert.AreEqual(res, final);
        }

        [Test]
        public void ClampOperator_With_Integer()
        {
            var clamp = ScriptableObject.CreateInstance<Operator.Clamp>();
            clamp.SetOperandType(0, typeof(int));
            clamp.SetOperandType(1, typeof(int));
            clamp.SetOperandType(2, typeof(int));
            Assert.AreEqual(VFXValueType.Int32, clamp.outputSlots[0].GetExpression().valueType);

            clamp.inputSlots[0].value = -6;
            clamp.inputSlots[1].value = -3;
            clamp.inputSlots[2].value = 4;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(clamp.outputSlots[0].GetExpression());
            var final = result.Get<int>();
            Assert.AreEqual(-3, final);
        }

        [Test]
        public void LengthOperator_With_Vector2()
        {
            var length = ScriptableObject.CreateInstance<Operator.Length>();
            length.SetOperandType(typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float, length.outputSlots[0].GetExpression().valueType);

            var vec2 = Vector2.one * 3;

            length.inputSlots[0].value = vec2;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(length.outputSlots[0].GetExpression());
            var final = result.Get<float>();
            Assert.AreEqual(vec2.magnitude, final);
        }

        [Test]
        public void DotProductOperator_With_Vector2()
        {
            var dot = ScriptableObject.CreateInstance<Operator.DotProduct>();
            dot.SetOperandType(typeof(Vector2));

            Assert.AreEqual(VFXValueType.Float, dot.outputSlots[0].GetExpression().valueType);

            var a = new Vector2(6, 7);
            var b = new Vector2(2, 3);

            dot.inputSlots[0].value = a;
            dot.inputSlots[1].value = b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(dot.outputSlots[0].GetExpression());
            var final = result.Get<float>();

            Assert.AreEqual(Vector2.Dot(a, b), final);
        }

        [Test]
        public void Cosine_With_Vector2()
        {
            var cos = ScriptableObject.CreateInstance<Operator.Cosine>();
            cos.SetOperandType(typeof(Vector2));

            Assert.AreEqual(VFXValueType.Float2, cos.outputSlots[0].GetExpression().valueType);
            var a = new Vector2(12, 89);

            cos.inputSlots[0].value = a;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(cos.outputSlots[0].GetExpression());
            var final = result.Get<Vector2>();

            Assert.AreEqual(Mathf.Cos(a.x), final.x);
            Assert.AreEqual(Mathf.Cos(a.y), final.y);
        }

        [Test]
        public void ModuloOperator_With_Unsigned_Integer()
        {
            var a = 1610612737u;
            var b = 805306361u;

            var moduloUInt = ScriptableObject.CreateInstance<Operator.Modulo>();
            moduloUInt.SetOperandType(typeof(uint));

            var moduloFloat = ScriptableObject.CreateInstance<Operator.Modulo>();
            moduloFloat.SetOperandType(typeof(float));

            var aOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            aOperator.SetSettingValue("m_Type", (SerializableType)typeof(uint));
            var bOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            bOperator.SetSettingValue("m_Type", (SerializableType)typeof(uint));

            aOperator.inputSlots[0].value = a;
            bOperator.inputSlots[0].value = b;

            moduloUInt.inputSlots[0].Link(aOperator.outputSlots.First());
            moduloUInt.inputSlots[1].Link(bOperator.outputSlots.First());

            moduloFloat.inputSlots[0].Link(aOperator.outputSlots.First());
            moduloFloat.inputSlots[1].Link(bOperator.outputSlots.First());

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultUInt = context.Compile(moduloUInt.outputSlots[0].GetExpression());
            var resultFloat = context.Compile(moduloFloat.outputSlots[0].GetExpression());

            Assert.AreEqual(a % b, resultUInt.Get<uint>());
            Assert.AreEqual(Mathf.Repeat(a, b), resultFloat.Get<float>());
        }

        [Test]
        public void MinimumOperator_Verify_Identity_Value()
        {
            var a = new Vector2(2, 2);
            var b = new Vector3(3, 3, 3);
            var e = new Vector3(2, 2, 3);

            var min = ScriptableObject.CreateInstance<Operator.Minimum>();
            min.SetOperandType(0, a.GetType());
            min.SetOperandType(1, b.GetType());

            min.inputSlots[0].value = a;
            min.inputSlots[1].value = b;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultVector3Expression = context.Compile(min.outputSlots[0].GetExpression());
            var r = resultVector3Expression.Get<Vector3>();

            Assert.AreEqual(e.x, r.x);
            Assert.AreEqual(e.y, r.y);
            Assert.AreEqual(e.z, r.z);
        }

        #pragma warning disable 0414
        private static Type[] swizzleType = new Type[] { typeof(Operator.Swizzle) };

        #pragma warning restore 0414
        [Test]
        public void SwizzleOperator([ValueSource("swizzleType")] Type swizzleType)
        {
            // check basic swizzle
            {
                var inputVec = ScriptableObject.CreateInstance<VFXOperatorVector2>();
                var swizzle = ScriptableObject.CreateInstance(swizzleType) as VFXOperator;
                if (swizzleType == typeof(Operator.Swizzle))
                {
                    (swizzle as Operator.Swizzle).SetOperandType(typeof(Vector2));
                }

                swizzle.inputSlots[0].Link(inputVec.outputSlots.First());
                swizzle.SetSettingValue("mask", "xxy");

                var finalExpr = swizzle.outputSlots.First().GetExpression();

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(finalExpr);
                var vec = result.Get<Vector3>();

                Assert.AreEqual(new Vector3(1.0f, 1.0f, 2.0f), vec);
            }

            // check out of bounds mask is clamped correctly
            {
                var inputVec = ScriptableObject.CreateInstance<VFXOperatorVector2>();
                var swizzle = ScriptableObject.CreateInstance(swizzleType) as VFXOperator;
                if (swizzleType == typeof(Operator.Swizzle))
                {
                    (swizzle as Operator.Swizzle).SetOperandType(typeof(Vector2));
                }
                swizzle.inputSlots[0].Link(inputVec.outputSlots.First());
                swizzle.SetSettingValue("mask", "yzx");

                var finalExpr = swizzle.outputSlots.First().GetExpression();

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(finalExpr);
                var vec = result.Get<Vector3>();

                Assert.AreEqual(new Vector3(2.0f, 2.0f, 1.0f), vec);
            }
        }

        [Test]
        public void AddOperator_MixingVector_And_Direction()
        {
            var add = ScriptableObject.CreateInstance<Operator.Add>();
            add.SetOperandType(0, typeof(Vector));
            add.SetOperandType(1, typeof(DirectionType));

            var a = new Vector3(1, 2, 3);
            var b = new Vector3(6, 5, 4);

            add.inputSlots[0].value = new Vector() { vector = a };
            add.inputSlots[1].value = new DirectionType() { direction = b };

            Assert.AreEqual(typeof(Vector), add.outputSlots[0].property.type);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultVector3Expression = context.Compile(add.outputSlots[0].GetExpression());
            var r = resultVector3Expression.Get<Vector3>();

            var e = a + b.normalized;
            Assert.AreEqual(e.x, r.x);
            Assert.AreEqual(e.y, r.y);
            Assert.AreEqual(e.z, r.z);
        }
    }
}
