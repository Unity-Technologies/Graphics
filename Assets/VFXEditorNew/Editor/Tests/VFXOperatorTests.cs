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
        class VFXOperatorFloatOne : VFXOperator
        {
            override public string name { get { return "Temp_Float_One"; } }
            override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
            {
                return new[] { new VFXValueFloat(1.0f, true) };
            }
        }

        [Test]
        public void CascadedAddOperator()
        {
            var one = new VFXOperatorFloatOne();
            var add = new VFXOperatorAdd();

            var count = 8.0f;
            for (int i = 0; i < (int)count; i++)
            {
                var emptySlot = add.inputSlots.First(s => !s.HasLink());
                emptySlot.Link(one.outputSlots.First());
            }

            var finalExpr = add.outputSlots.First().expression;

            var context = new VFXExpression.Context();
            var result = context.Compile(finalExpr);
            var eight = result.GetContent<float>();

            Assert.AreEqual(count, eight);
        }

        /*TODOPAUL
        [Test]
        public void CascadedMulOperator()
        {
            var one = new VFXOperatorFloatOne();
            var two = new VFXOperatorAdd();

            two.ConnectInput(two.inputSlots[0].id, one, one.outputSlots[0].id);
            two.ConnectInput(two.inputSlots[1].id, one, one.outputSlots[0].id);

            var vec2_Two = new VFXOperatorAppendVector();
            vec2_Two.ConnectInput(vec2_Two.inputSlots[0].id, two, two.outputSlots[0].id);
            vec2_Two.ConnectInput(vec2_Two.inputSlots[1].id, two, two.outputSlots[0].id);

            var vec3_Two = new VFXOperatorAppendVector();
            vec3_Two.ConnectInput(vec3_Two.inputSlots[0].id, vec2_Two, vec2_Two.outputSlots[0].id);
            vec3_Two.ConnectInput(vec3_Two.inputSlots[1].id, two, two.outputSlots[0].id);

            var mul = new VFXOperatorMul();
            mul.ConnectInput(mul.inputSlots[0].id, vec2_Two, vec2_Two.outputSlots[0].id);
            mul.ConnectInput(mul.inputSlots[1].id, vec3_Two, vec3_Two.outputSlots[0].id);

            var context = new VFXExpression.Context();
            var result = context.Compile(mul.outputSlots[0].expression);
            var final = result.GetContent<Vector3>();

            Assert.AreEqual(final, new Vector3(4, 4, 2));
        }

        [Test]
        public void ChangeTypeInCascade()
        {
            var one = new VFXOperatorFloatOne();

            var vec2_One = new VFXOperatorAppendVector();
            vec2_One.ConnectInput(vec2_One.inputSlots[0].id, one, one.outputSlots[0].id);
            vec2_One.ConnectInput(vec2_One.inputSlots[1].id, one, one.outputSlots[0].id);

            var vec3_One = new VFXOperatorAppendVector();
            vec3_One.ConnectInput(vec3_One.inputSlots[0].id, vec2_One, vec2_One.outputSlots[0].id);
            vec3_One.ConnectInput(vec3_One.inputSlots[1].id, one, one.outputSlots[0].id);

            var cos = new VFXOperatorCos();
            cos.ConnectInput(cos.inputSlots[0].id, vec2_One, vec2_One.outputSlots[0].id);

            var sin = new VFXOperatorSin();
            sin.ConnectInput(sin.inputSlots[0].id, cos, cos.outputSlots[0].id);

            var abs = new VFXOperatorAbs();
            abs.ConnectInput(abs.inputSlots[0].id, sin, sin.outputSlots[0].id);
            Assert.AreEqual(abs.outputSlots[0].expression.ValueType, VFXValueType.kFloat2);

            //Cascaded invalidation should occurs
            cos.ConnectInput(cos.inputSlots[0].id, vec3_One, vec3_One.outputSlots[0].id);
            Assert.AreEqual(abs.outputSlots[0].expression.ValueType, VFXValueType.kFloat3);
        }

        [Test]
        public void AutoDisconnectInvalid()
        {
            var one = new VFXOperatorFloatOne();

            var append = new VFXOperatorAppendVector();
            append.ConnectInput(append.inputSlots[0].id, one, one.outputSlots[0].id);
            append.ConnectInput(append.inputSlots[1].id, one, one.outputSlots[0].id);
            append.ConnectInput(append.inputSlots[2].id, one, one.outputSlots[0].id);

            var cross = new VFXOperatorCross();
            cross.ConnectInput(cross.inputSlots[0].id, append, append.outputSlots[0].id);

            Assert.AreNotEqual(cross.inputSlots[0].parent, null);

            append.DisconnectInput(append.inputSlots[2].id);
            Assert.AreEqual(cross.inputSlots[0].parent, null);
        }
        */
    }
}
