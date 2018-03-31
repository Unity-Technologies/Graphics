using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXOperatorNewTests
    {
        [Test]
        public void CascadedAddNewOperatorCascadedBehavior()
        {
            var one = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var add = ScriptableObject.CreateInstance<Operator.AddNew>();

            one.SetSettingValue("m_Type", (SerializableType)typeof(float));
            one.inputSlots[0].value = 1.0f;

            var count = 8.0f;
            for (int i = 0; i < (int)count; i++)
            {
                add.AddOperand();
            }

            for (int i = 0; i < (int)count; i++)
            {
                var inputSlots = add.inputSlots.ToArray();
                var emptySlot = inputSlots.First(s => !s.HasLink());
                emptySlot.Link(one.outputSlots.First());
            }

            var finalExpr = add.outputSlots.First().GetExpression();

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(finalExpr);
            var eight = result.Get<float>();

            Assert.AreEqual(count, eight);
        }

        [Test]
        public void CascadedMulNewOperatorDifferentFloatSizes()
        {
            var vec2_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var vec3_Two = ScriptableObject.CreateInstance<VFXInlineOperator>();

            vec2_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector2));
            vec2_Two.inputSlots[0].value = Vector2.one * 2.0f;

            vec3_Two.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            vec3_Two.inputSlots[0].value = Vector3.one * 2.0f;

            var mul = ScriptableObject.CreateInstance<Operator.MultiplyNew>();
            mul.SetOperandType(0, VFXValueType.Float2);
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.SetOperandType(1, VFXValueType.Float3);
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(mul.outputSlots[0].GetExpression());
            var final = result.Get<Vector3>();

            Assert.AreEqual(new Vector3(4, 4, 2), final);
        }

        [Test]
        public void CascadedSubstractIntegerBehavior()
        {
            var subtract = ScriptableObject.CreateInstance<Operator.SubtractNew>();
            subtract.SetOperandType(0, VFXValueType.Float);
            subtract.SetOperandType(1, VFXValueType.Float);

            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, VFXValueType.Float);
            subtract.SetOperandType(1, VFXValueType.Int32);
            Assert.AreEqual(VFXValueType.Float, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, VFXValueType.Int32);
            subtract.SetOperandType(1, VFXValueType.Int32);
            Assert.AreEqual(VFXValueType.Int32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, VFXValueType.Int32);
            subtract.SetOperandType(1, VFXValueType.Uint32);
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            subtract.SetOperandType(0, VFXValueType.Uint32);
            subtract.SetOperandType(1, VFXValueType.Uint32);
            Assert.AreEqual(VFXValueType.Uint32, subtract.outputSlots[0].GetExpression().valueType);

            //Finally, do some simple math integer (TODOPAUL : are incorrect mathematically but another PR is coming for this support)
            subtract.SetOperandType(0, VFXValueType.Int32);
            subtract.SetOperandType(1, VFXValueType.Uint32);

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
        public void ClampNewBehavior()
        {
            var clamp = ScriptableObject.CreateInstance<Operator.ClampNew>();
            clamp.SetOperandType(VFXValueType.Int32);
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
        public void LengthNewBehavior()
        {
            var clamp = ScriptableObject.CreateInstance<Operator.LengthNew>();
            clamp.SetOperandType(VFXValueType.Float2);
            Assert.AreEqual(VFXValueType.Float, clamp.outputSlots[0].GetExpression().valueType);

            var vec2 = Vector2.one * 3;

            clamp.inputSlots[0].value = vec2;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(clamp.outputSlots[0].GetExpression());
            var final = result.Get<float>();
            Assert.AreEqual(vec2.magnitude, final);
        }
    }
}
