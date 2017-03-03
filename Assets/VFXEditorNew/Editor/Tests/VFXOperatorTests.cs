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

        [Test]
        public void CascadedMulOperator()
        {
            var one = new VFXOperatorFloatOne();
            var two = new VFXOperatorAdd();

            two.inputSlots[0].Link(one.outputSlots[0]);
            two.inputSlots[1].Link(one.outputSlots[0]);

            var vec2_Two = new VFXOperatorAppendVector();
            vec2_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec2_Two.inputSlots[1].Link(two.outputSlots[0]);

            var vec3_Two = new VFXOperatorAppendVector();
            vec3_Two.inputSlots[0].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[1].Link(two.outputSlots[0]);
            vec3_Two.inputSlots[2].Link(two.outputSlots[0]);

            var mul = new VFXOperatorMul();
            mul.inputSlots[0].Link(vec2_Two.outputSlots[0]);
            mul.inputSlots[1].Link(vec3_Two.outputSlots[0]);

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
            vec2_One.inputSlots[0].Link(one.outputSlots[0]);
            vec2_One.inputSlots[1].Link(one.outputSlots[0]);

            var vec3_One = new VFXOperatorAppendVector();
            vec3_One.inputSlots[0].Link(vec2_One.outputSlots[0]);
            vec3_One.inputSlots[1].Link(one.outputSlots[0]);

            var cos = new VFXOperatorCos();
            cos.inputSlots[0].Link(vec2_One.outputSlots[0]);

            var sin = new VFXOperatorSin();
            sin.inputSlots[0].Link(cos.outputSlots[0]);

            var abs = new VFXOperatorAbs();
            abs.inputSlots[0].Link(sin.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].expression.ValueType, VFXValueType.kFloat2);

            //Cascaded invalidation should occurs
            cos.inputSlots[0].Link(vec3_One.outputSlots[0]);
            Assert.AreEqual(abs.outputSlots[0].expression.ValueType, VFXValueType.kFloat3);
        }

        /*TODOPAUL
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
