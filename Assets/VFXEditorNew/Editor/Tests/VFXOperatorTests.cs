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
                var emptySlot = add.InputSlots.First(s => s.parent == null);
                add.ConnectInput(emptySlot.slotID, one, one.OutputSlots.First().slotID);
            }

            var finalExpr = add.OutputSlots.First().expression;

            var context = new VFXExpression.Context();
            var result = context.Compile(finalExpr);
            var eight = result.GetContent<float>();

            Assert.AreEqual(eight, count);
        }

        [Test]
        public void CascadedMulOperator()
        {
            var one = new VFXOperatorFloatOne();
            var two = new VFXOperatorAdd();
            two.ConnectInput(two.InputSlots[0].slotID, one, one.OutputSlots[0].slotID);
            two.ConnectInput(two.InputSlots[1].slotID, one, one.OutputSlots[0].slotID);

            var vec2_Two = new VFXOperatorAppendVector();
            vec2_Two.ConnectInput(vec2_Two.InputSlots[0].slotID, two, two.OutputSlots[0].slotID);
            vec2_Two.ConnectInput(vec2_Two.InputSlots[1].slotID, two, two.OutputSlots[0].slotID);

            var vec3_Two = new VFXOperatorAppendVector();
            vec3_Two.ConnectInput(vec3_Two.InputSlots[0].slotID, vec2_Two, vec2_Two.OutputSlots[0].slotID);
            vec3_Two.ConnectInput(vec3_Two.InputSlots[1].slotID, two, two.OutputSlots[0].slotID);

            var mul = new VFXOperatorMul();
            mul.ConnectInput(mul.InputSlots[0].slotID, vec2_Two, vec2_Two.OutputSlots[0].slotID);
            mul.ConnectInput(mul.InputSlots[1].slotID, vec3_Two, vec3_Two.OutputSlots[0].slotID);

            var context = new VFXExpression.Context();
            var result = context.Compile(mul.OutputSlots[0].expression);
            var final = result.GetContent<Vector3>();

            Assert.AreEqual(final, new Vector3(4, 4, 2));
        }

        [Test]
        public void ChangeTypeInCascade()
        {
            var one = new VFXOperatorFloatOne();

            var vec2_One = new VFXOperatorAppendVector();
            vec2_One.ConnectInput(vec2_One.InputSlots[0].slotID, one, one.OutputSlots[0].slotID);
            vec2_One.ConnectInput(vec2_One.InputSlots[1].slotID, one, one.OutputSlots[0].slotID);

            var vec3_One = new VFXOperatorAppendVector();
            vec3_One.ConnectInput(vec3_One.InputSlots[0].slotID, vec2_One, vec2_One.OutputSlots[0].slotID);
            vec3_One.ConnectInput(vec3_One.InputSlots[1].slotID, one, one.OutputSlots[0].slotID);

            var cos = new VFXOperatorCos();
            cos.ConnectInput(cos.InputSlots[0].slotID, vec2_One, vec2_One.OutputSlots[0].slotID);

            var sin = new VFXOperatorSin();
            sin.ConnectInput(sin.InputSlots[0].slotID, cos, cos.OutputSlots[0].slotID);

            var abs = new VFXOperatorAbs();
            abs.ConnectInput(abs.InputSlots[0].slotID, sin, sin.OutputSlots[0].slotID);
            Assert.AreEqual(abs.OutputSlots[0].expression.ValueType, VFXValueType.kFloat2);

            //Cascaded invalidation should occurs
            cos.ConnectInput(cos.InputSlots[0].slotID, vec3_One, vec3_One.OutputSlots[0].slotID);
            Assert.AreEqual(abs.OutputSlots[0].expression.ValueType, VFXValueType.kFloat3);
        }

        [Test]
        public void AutoDisconnectInvalid()
        {
            var one = new VFXOperatorFloatOne();

            var append = new VFXOperatorAppendVector();
            append.ConnectInput(append.InputSlots[0].slotID, one, one.OutputSlots[0].slotID);
            append.ConnectInput(append.InputSlots[1].slotID, one, one.OutputSlots[0].slotID);
            append.ConnectInput(append.InputSlots[2].slotID, one, one.OutputSlots[0].slotID);

            var cross = new VFXOperatorCross();
            cross.ConnectInput(cross.InputSlots[0].slotID, append, append.OutputSlots[0].slotID);

            Assert.AreNotEqual(cross.InputSlots[0].parent, null);

            append.DisconnectInput(append.InputSlots[2].slotID);
            Assert.AreEqual(cross.InputSlots[0].parent, null);
        }
    }
}
