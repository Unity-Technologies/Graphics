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

        [Test]
        public void SphereSpaceConversionUnexpected()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0].space = CoordinateSpace.Global;
            sphere_B.inputSlots[0].space = CoordinateSpace.Local;

            Assert.AreEqual(CoordinateSpace.Global, sphere_A.outputSlots[0].space);
            Assert.AreEqual(CoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][1].Link(sphere_A.outputSlots[0][1]); //link radius to other radius
            Assert.AreEqual(CoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(sphere_B.outputSlots[0][0].GetExpression());
            var resultRadius = context.Compile(sphere_B.outputSlots[0][1].GetExpression());

            var allExprCenter = CollectExpression(resultCenter).ToArray();
            var allExprRadius = CollectExpression(resultRadius).ToArray();

            Assert.IsFalse(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsFalse(allExprRadius.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }

        [Test]
        public void SphereSpaceConversionExpected()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            sphere_A.inputSlots[0].space = CoordinateSpace.Global;
            sphere_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            sphere_B.inputSlots[0].space = CoordinateSpace.Local;

            Assert.AreEqual(CoordinateSpace.Global, sphere_A.outputSlots[0].space);
            Assert.AreEqual(CoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][0].Link(sphere_A.outputSlots[0][0]); //link sphere center to other sphere center
            Assert.AreEqual(CoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var allExprCenter = CollectExpression(sphere_B.outputSlots[0][0].GetExpression()).ToArray();
            var allExprRadius = CollectExpression(sphere_B.outputSlots[0][1].GetExpression()).ToArray();

            Assert.IsTrue(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsFalse(allExprRadius.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }
    }
}
