using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXOperatorTests
    {
        [Test]
        public void CascadedAddOperator()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();

            var count = 8.0f;
            for (int i = 0; i < (int)count; i++)
            {
                var inputSlots = add.inputSlots.ToArray();
                var emptySlot = inputSlots.First(s => !s.HasLink());
                emptySlot.Link(one.outputSlots.First());
            }

            var finalExpr = add.outputSlots.First().GetExpression();

            var context = new VFXExpression.Context(VFXExpression.Context.ReductionOption.CPUEvaluation);
            var result = context.Compile(finalExpr);
            var eight = result.Get<float>();

            Assert.AreEqual(count, eight);
        }

        [Test]
        public void CascadedMulOperator()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var two = ScriptableObject.CreateInstance<VFXOperatorAdd>();

            two.inputSlots[0].Link(one.outputSlots[0]);
            two.inputSlots[1].Link(one.outputSlots[0]);

            var vec2_Two = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            vec2_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec2_Two.inputSlots[1].Link(two.outputSlots[0]);

            var vec3_Two = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            vec3_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[1].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[2].Link(two.outputSlots[0]);

            var mul = ScriptableObject.CreateInstance<VFXOperatorMul>();
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

            var context = new VFXExpression.Context(VFXExpression.Context.ReductionOption.CPUEvaluation);
            var result = context.Compile(mul.outputSlots[0].GetExpression());
            var final = result.Get<Vector3>();

            Assert.AreEqual(final, new Vector3(4, 4, 2));
        }

        [Test]
        public void ChangeTypeInCascade()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();

            var vec2_One = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            vec2_One.inputSlots[0].Link(one.outputSlots[0]);
            vec2_One.inputSlots[1].Link(one.outputSlots[0]);
            Assert.AreEqual(vec2_One.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat2);

            var vec3_One = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            vec3_One.inputSlots[0].Link(vec2_One.outputSlots[0]);
            vec3_One.inputSlots[1].Link(one.outputSlots[0]);
            Assert.AreEqual(vec3_One.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat3);

            var cos = ScriptableObject.CreateInstance<VFXOperatorCos>();
            cos.inputSlots[0].Link(vec2_One.outputSlots[0]);
            Assert.AreEqual(cos.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat2);

            var sin = ScriptableObject.CreateInstance<VFXOperatorSin>();
            sin.inputSlots[0].Link(cos.outputSlots[0]);
            Assert.AreEqual(sin.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat2);

            var abs = ScriptableObject.CreateInstance<VFXOperatorAbs>();
            abs.inputSlots[0].Link(sin.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat2);

            //Cascaded invalidation should occurs
            cos.inputSlots[0].Link(vec3_One.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].GetExpression().ValueType, VFXValueType.kFloat3);
        }

        [Test]
        public void AutoDisconnectInvalid()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();

            var append = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            append.inputSlots[0].Link(one.outputSlots[0]);
            append.inputSlots[1].Link(one.outputSlots[0]);
            append.inputSlots[2].Link(one.outputSlots[0]);

            var cross = ScriptableObject.CreateInstance<VFXOperatorCross>();
            cross.inputSlots[0].Link(append.outputSlots[0]);
            Assert.IsTrue(cross.inputSlots[0].HasLink());

            append.inputSlots[2].UnlinkAll();
            Assert.IsFalse(cross.inputSlots[0].HasLink());
        }

        [Test]
        public void Append()
        {
            var one = ScriptableObject.CreateInstance<VFXOperatorFloatOne>();
            var append = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            append.inputSlots[0].Link(one.outputSlots[0]);
            append.inputSlots[1].Link(one.outputSlots[0]);
            append.inputSlots[2].Link(one.outputSlots[0]);
            append.inputSlots[3].Link(one.outputSlots[0]);

            var expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.kFloat4, expression.ValueType);
        }

        [Test]
        public void ComponentMaskAndAppend()
        {
            var componentMask = ScriptableObject.CreateInstance<VFXOperatorComponentMask>();
            componentMask.settings = new VFXOperatorComponentMask.Settings() { mask = "xy" };
            var expression = componentMask.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.kFloat2, expression.ValueType);

            var append = ScriptableObject.CreateInstance<VFXOperatorAppendVector>();
            append.inputSlots[0].Link(componentMask.outputSlots[0]);
            append.inputSlots[1].Link(componentMask.outputSlots[0]);
            expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.kFloat4, expression.ValueType);

            componentMask.settings = new VFXOperatorComponentMask.Settings() { mask = "x" };
            expression = append.outputSlots[0].GetExpression();
            Assert.AreEqual(VFXValueType.kFloat2, expression.ValueType);
        }

        [Test]
        public void ReferenceOfAttributeEquality()
        {
            foreach (var attribute in VFXAttributeExpression.All)
            {
                var desc = VFXLibrary.GetAttributeParameters().First(p => p.name == attribute);
                var a = desc.CreateInstance();
                var b = desc.CreateInstance();
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                Assert.AreNotEqual(a, b);

                var reference = VFXAttributeExpression.Find(attribute);
                Assert.IsTrue(ReferenceEquals(reference, a.outputSlots[0].GetExpression()));
                Assert.IsTrue(ReferenceEquals(reference, b.outputSlots[0].GetExpression()));
            }
        }

        [Test]
        public void ReferenceOfBuiltInEquality()
        {
            foreach (var operation in VFXBuiltInExpression.All)
            {
                var desc = VFXLibrary.GetBuiltInParameters().First(p => p.name == operation.ToString());
                var a = desc.CreateInstance();
                var b = desc.CreateInstance();
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                Assert.AreNotEqual(a, b);

                var reference = VFXBuiltInExpression.Find(operation);
                Assert.IsTrue(ReferenceEquals(reference, a.outputSlots[0].GetExpression()));
                Assert.IsTrue(ReferenceEquals(reference, b.outputSlots[0].GetExpression()));
            }
        }
    }
}
