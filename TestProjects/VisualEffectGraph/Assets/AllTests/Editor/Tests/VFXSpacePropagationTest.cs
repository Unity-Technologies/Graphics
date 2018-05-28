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
        static IEnumerable<VFXExpression> CollectExpression(VFXExpression expression, HashSet<VFXExpression> hashSet = null)
        {
            if (hashSet == null)
            {
                hashSet = new HashSet<VFXExpression>();
            }

            if (!hashSet.Contains(expression))
            {
                hashSet.Add(expression);
                yield return expression;
                foreach (var parent in expression.parents)
                {
                    var parents = CollectExpression(parent, hashSet);
                    foreach (var exp in parents)
                    {
                        yield return exp;
                    }
                }
            }
        }

        [Test]
        public void Sphere_Type_Should_Be_Spaceable()
        {
            var inline = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inline.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            Assert.IsTrue(inline.inputSlots[0].spaceable);
        }

        [Test]
        public void SpaceUniformisation_Between_World_And_Local()
        {
            var add = ScriptableObject.CreateInstance<Add>();
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
        public void SpaceTransmission_From_An_Operator_To_Another([ValueSource("SpaceTransmissionType")] Type type)
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
        public void SpaceConversion_Sphere_Unexpected_Linking_MasterSlot()
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
        public void SpaceConversion_Sphere_Expected_Linking_Subslot()
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

        [Test]
        public void SpaceConversion_Position_Unexpected_Linking_Subslot()
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            position_B.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            position_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            position_A.inputSlots[0].space = CoordinateSpace.Global;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = CoordinateSpace.Local;

            for (int i = 0; i < 3; ++i)
            {
                position_A.outputSlots[0][0][i].Link(position_B.inputSlots[0][0][i]);
            }

            var allExprPosition = CollectExpression(position_B.outputSlots[0][0].GetExpression()).ToArray();

            Assert.AreEqual(CoordinateSpace.Global, position_A.outputSlots[0].space);
            Assert.AreEqual(CoordinateSpace.Local, position_B.outputSlots[0].space);
            Assert.IsFalse(allExprPosition.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
        }

        [Test]
        public void SpaceConversion_Conversion_Expected_Linking_Position_To_Line()
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            position_B.SetSettingValue("m_Type", (SerializableType)typeof(Position));

            position_A.inputSlots[0][0].value = new Vector3(1, 2, 3);
            position_A.inputSlots[0].space = CoordinateSpace.Global;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = CoordinateSpace.Local;

            var line = ScriptableObject.CreateInstance<VFXInlineOperator>();
            line.SetSettingValue("m_Type", (SerializableType)typeof(Line));

            line.inputSlots[0].space = CoordinateSpace.Local;
            line.inputSlots[0][0].Link(position_A.outputSlots[0]);
            line.inputSlots[0][1].Link(position_B.outputSlots[0]);

            var lineOutputSlotA = CollectExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            var lineOutputSlotB = CollectExpression(line.outputSlots[0][1].GetExpression()).ToArray();

            Assert.AreEqual(line.inputSlots[0].space, CoordinateSpace.Local);
            Assert.IsTrue(lineOutputSlotA.Any(o => o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsFalse(lineOutputSlotB.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            line.inputSlots[0].space = CoordinateSpace.Global;
            lineOutputSlotA = CollectExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            lineOutputSlotB = CollectExpression(line.outputSlots[0][1].GetExpression()).ToArray();
            Assert.AreEqual(line.inputSlots[0].space, CoordinateSpace.Global);
            Assert.IsFalse(lineOutputSlotA.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsTrue(lineOutputSlotB.Any(o => o.operation == VFXExpressionOperation.LocalToWorld));
        }
    }
}
