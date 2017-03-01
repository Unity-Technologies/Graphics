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
        public void BasicExpressionGraph()
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
    }
}
