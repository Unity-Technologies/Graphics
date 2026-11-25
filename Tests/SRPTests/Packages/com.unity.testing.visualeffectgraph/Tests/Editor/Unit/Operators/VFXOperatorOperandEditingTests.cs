using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    public class VFXOperatorOperandEditingTests
    {
        [Test]
        public void CascadedAddOperator_Adding_And_Removing_Several_Links()
        {
            var one = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var add = ScriptableObject.CreateInstance<Operator.Add>();

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

            //< Process some remove
            var removeCount = 4.0f;
            for (int i = 0; i < (int)removeCount; ++i)
            {
                var linkedSlots = add.inputSlots.Where(o => o.HasLink());
                VFXSlot removeSlot = null;
                if (i % 2 == 0)
                {
                    removeSlot = linkedSlots.First();
                }
                else
                {
                    removeSlot = linkedSlots.Last();
                }

                //Check expected link count
                var linkCount = add.inputSlots.Where(o => o.HasLink()).Count();
                Assert.AreEqual((int)count - i, linkCount);

                var index = add.inputSlots.IndexOf(removeSlot);
                add.RemoveOperand(index);

                //Check expected link count
                linkCount = add.inputSlots.Where(o => o.HasLink()).Count();
                Assert.AreEqual((int)count - i - 1, linkCount);

                //Check if all input slot are still with a default value
                foreach (var slot in add.inputSlots.Where(o => o.HasLink()))
                {
                    Assert.AreEqual(0.0f, slot.value);
                }

                //Check computed result
                finalExpr = add.outputSlots.First().GetExpression();
                result = context.Compile(finalExpr);
                var res = result.Get<float>();
                Assert.AreEqual(count - (float)i - 1.0f, res);
            }
        }

        [Test]
        public void AppendOperator()
        {
            var append = ScriptableObject.CreateInstance<Operator.AppendVector>();
            Assert.AreEqual(VFXValueType.Float2, append.outputSlots[0].GetExpression().valueType);
            append.AddOperand();
            Assert.AreEqual(VFXValueType.Float3, append.outputSlots[0].GetExpression().valueType);
            append.AddOperand();
            Assert.AreEqual(VFXValueType.Float4, append.outputSlots[0].GetExpression().valueType);

            append.RemoveOperand();
            append.RemoveOperand();

            Assert.AreEqual(VFXValueType.Float2, append.outputSlots[0].GetExpression().valueType);
            append.SetOperandType(0, typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float3, append.outputSlots[0].GetExpression().valueType);
            append.SetOperandType(1, typeof(Vector2));
            Assert.AreEqual(VFXValueType.Float4, append.outputSlots[0].GetExpression().valueType);
        }

        [Test]
        public void AppendOperator_With_Direction()
        {
            var append = ScriptableObject.CreateInstance<Operator.AppendVector>();
            append.SetOperandType(0, typeof(DirectionType));
            append.SetOperandType(1, typeof(float));

            append.inputSlots[0].value = new DirectionType() { direction = new Vector3(1.0f, 1.0f, 0) };
            append.inputSlots[1].value = 3.0f;

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var outputValue = context.Compile(append.outputSlots[0].GetExpression());

            var expressionValue = outputValue.Get<Vector4>();
            //Direction expects a normalize
            var expectedValue = new Vector4(1.0f / Mathf.Sqrt(2), 1.0f / Mathf.Sqrt(2), 0.0f, 3.0f);

            Assert.AreEqual(expectedValue.x, expressionValue.x, 1e-5f);
            Assert.AreEqual(expectedValue.y, expressionValue.y, 1e-5f);
            Assert.AreEqual(expectedValue.z, expressionValue.z, 1e-5f);
            Assert.AreEqual(expectedValue.w, expressionValue.w, 1e-5f);
        }
    }
}
