using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX.Operator;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSpacePropagationTest
    {
        static IEnumerable<VFXExpression> CollectExpression(VFXExpression startExpression)
        {
            yield return startExpression;
            foreach (var parent in startExpression.parents)
            {
                var parents = CollectExpression(parent);
                foreach (var exp in parents)
                {
                    yield return exp;
                }
            }
        }

        [Test]
        public void ExpectedTypeSpaceable()
        {
            var inline = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inline.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            Assert.IsTrue(inline.inputSlots[0].spaceable);
        }

        [Test]
        public void SpaceUniformisation()
        {
            var add = ScriptableObject.CreateInstance<AddNew>();
            add.SetOperandType(0, typeof(Position));
            add.SetOperandType(1, typeof(Position));

            Assert.AreEqual(add.outputSlots[0].property.type, typeof(Position));
            Assert.AreEqual(add.outputSlots[0].space, CoordinateSpace.Local);

            add.inputSlots[0].space = CoordinateSpace.Global;
            Assert.AreEqual(add.inputSlots[0].space, CoordinateSpace.Global);
            Assert.AreEqual(add.inputSlots[1].space, CoordinateSpace.Local);
            Assert.AreEqual(add.outputSlots[0].space, CoordinateSpace.Global);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(add.outputSlots[0].GetExpression());

            var allExpr = CollectExpression(result).ToArray();
            Assert.IsTrue(allExpr.Any(o =>
            {
                return o.operation == VFXExpressionOperation.LocalToWorld;
            }));
        }

        private static Type[] SpaceTransmissionType = { typeof(Position), typeof(Sphere) };
        [Test]
        public void SpaceTransmission([ValueSource("SpaceTransmissionType")] Type type)
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)type);
            position_B.SetSettingValue("m_Type", (SerializableType)type);

            position_A.inputSlots[0].space = CoordinateSpace.Global;
            Assert.AreEqual(CoordinateSpace.Global, position_A.outputSlots[0].space);

            position_B.inputSlots[0].space = CoordinateSpace.Local;
            Assert.AreEqual(CoordinateSpace.Local, position_B.outputSlots[0].space);

            position_B.inputSlots[0].Link(position_A.outputSlots[0]);
            Assert.AreEqual(CoordinateSpace.Global, position_B.outputSlots[0].space);

            position_A.inputSlots[0].space = CoordinateSpace.Local;
            Assert.AreEqual(CoordinateSpace.Local, position_B.outputSlots[0].space);
        }
    }
}
