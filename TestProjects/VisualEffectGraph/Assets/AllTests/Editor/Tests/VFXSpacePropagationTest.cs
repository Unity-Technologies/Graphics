using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX.Operator;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSpacePropagationTest
    {
        static IEnumerable<VFXExpression> CollectParentExpression(VFXExpression expression, HashSet<VFXExpression> hashSet = null)
        {
            if (expression != null)
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
                        var parents = CollectParentExpression(parent, hashSet);
                        foreach (var exp in parents)
                        {
                            yield return exp;
                        }
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
            Assert.AreEqual(add.outputSlots[0].space, VFXCoordinateSpace.Local);

            add.inputSlots[0].space = VFXCoordinateSpace.Global;
            Assert.AreEqual(add.inputSlots[0].space, VFXCoordinateSpace.Global);
            Assert.AreEqual(add.inputSlots[1].space, VFXCoordinateSpace.Local);
            Assert.AreEqual(add.outputSlots[0].space, VFXCoordinateSpace.Global);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var result = context.Compile(add.outputSlots[0].GetExpression());

            var allExpr = CollectParentExpression(result).ToArray();
            Assert.IsTrue(allExpr.Count(o =>
                {
                    return o.operation == VFXExpressionOperation.LocalToWorld;
                }) == 1);
        }

        #pragma warning disable 0414
        private static Type[] SpaceTransmissionType = { typeof(Position), typeof(Sphere) };

        #pragma warning restore 0414
        [Test]
        public void SpaceTransmission_From_An_Operator_To_Another([ValueSource("SpaceTransmissionType")] Type type)
        {
            var position_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var position_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            position_A.SetSettingValue("m_Type", (SerializableType)type);
            position_B.SetSettingValue("m_Type", (SerializableType)type);

            position_A.inputSlots[0].space = VFXCoordinateSpace.Global;
            Assert.AreEqual(VFXCoordinateSpace.Global, position_A.outputSlots[0].space);

            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);

            position_B.inputSlots[0].Link(position_A.outputSlots[0]);
            Assert.AreEqual(VFXCoordinateSpace.Global, position_B.outputSlots[0].space);

            position_A.inputSlots[0].space = VFXCoordinateSpace.Local;
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);
        }

        [Test]
        public void SpaceConversion_Vector3_To_ArcSphere_Center_DoesntExcept_Conversion()
        {
            var rotate3D = ScriptableObject.CreateInstance<Rotate3D>();
            var arcSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();

            arcSphere.SetSettingValue("m_Type", (SerializableType)typeof(ArcSphere));

            arcSphere.inputSlots[0][0][0].Link(rotate3D.outputSlots[0]); //link result of rotate3D to center of arcSphere

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(arcSphere.inputSlots[0][0][0].GetExpression());

            var allExprCenter = CollectParentExpression(resultCenter).ToArray();
            Assert.IsFalse(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal)); //everything is within the same space by default

            arcSphere.inputSlots[0].space = VFXCoordinateSpace.Global;

            resultCenter = context.Compile(arcSphere.inputSlots[0][0][0].GetExpression());
            allExprCenter = CollectParentExpression(resultCenter).ToArray();

            Assert.IsTrue(allExprCenter.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Sphere_Unexpected_Linking_MasterSlot()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0].space = VFXCoordinateSpace.Global;
            sphere_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            Assert.AreEqual(VFXCoordinateSpace.Global, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][1].Link(sphere_A.outputSlots[0][1]); //link radius to other radius
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(sphere_B.outputSlots[0][0].GetExpression());
            var resultRadius = context.Compile(sphere_B.outputSlots[0][1].GetExpression());

            var allExprCenter = CollectParentExpression(resultCenter).ToArray();
            var allExprRadius = CollectParentExpression(resultRadius).ToArray();

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
            sphere_A.inputSlots[0].space = VFXCoordinateSpace.Global;
            sphere_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            sphere_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            Assert.AreEqual(VFXCoordinateSpace.Global, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][0].Link(sphere_A.outputSlots[0][0]); //link sphere center to other sphere center
            Assert.AreEqual(VFXCoordinateSpace.Local, sphere_B.outputSlots[0].space);

            var allExprCenter = CollectParentExpression(sphere_B.outputSlots[0][0].GetExpression()).ToArray();
            var allExprRadius = CollectParentExpression(sphere_B.outputSlots[0][1].GetExpression()).ToArray();

            Assert.IsTrue(allExprCenter.Count(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal) == 1);
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
            position_A.inputSlots[0].space = VFXCoordinateSpace.Global;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            for (int i = 0; i < 3; ++i)
            {
                position_A.outputSlots[0][0][i].Link(position_B.inputSlots[0][0][i]);
            }

            var allExprPosition = CollectParentExpression(position_B.outputSlots[0][0].GetExpression()).ToArray();

            Assert.AreEqual(VFXCoordinateSpace.Global, position_A.outputSlots[0].space);
            Assert.AreEqual(VFXCoordinateSpace.Local, position_B.outputSlots[0].space);
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
            position_A.inputSlots[0].space = VFXCoordinateSpace.Global;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXCoordinateSpace.Local;

            var line = ScriptableObject.CreateInstance<VFXInlineOperator>();
            line.SetSettingValue("m_Type", (SerializableType)typeof(Line));

            line.inputSlots[0].space = VFXCoordinateSpace.Local;
            line.inputSlots[0][0].Link(position_A.outputSlots[0]);
            line.inputSlots[0][1].Link(position_B.outputSlots[0]);

            var lineOutputSlotA = CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            var lineOutputSlotB = CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();

            Assert.AreEqual(line.inputSlots[0].space, VFXCoordinateSpace.Local);
            Assert.IsTrue(lineOutputSlotA.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);
            Assert.IsFalse(lineOutputSlotB.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            line.inputSlots[0].space = VFXCoordinateSpace.Global;
            lineOutputSlotA = CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            lineOutputSlotB = CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();
            Assert.AreEqual(line.inputSlots[0].space, VFXCoordinateSpace.Global);
            Assert.IsFalse(lineOutputSlotA.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsTrue(lineOutputSlotB.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Conversion_Expected_Between_Slot_Block_And_Context()
        {
            var initializeContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var positionSphere = ScriptableObject.CreateInstance<PositionSphere>();
            initializeContext.AddChild(positionSphere);

            //Default is expected to be in same space between block & context
            var slotSpherePositionExpressions = CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Any());
            Assert.IsFalse(slotSpherePositionExpressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Now switch space of block
            initializeContext.space = VFXCoordinateSpace.Global;
            slotSpherePositionExpressions = CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Verify_Expected_Invalidation_Of_Space()
        {
            var inlineVector = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineVector.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));

            var transformSpace = ScriptableObject.CreateInstance<TransformSpace>();
            transformSpace.SetOperandType(typeof(Position));
            transformSpace.outputSlots[0].Link(inlineVector.inputSlots[0]);

            //Local => Local
            var slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.AreEqual(VFXCoordinateSpace.Local, transformSpace.inputSlots[0].space);
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //World => Local
            transformSpace.inputSlots[0].space = VFXCoordinateSpace.Global;
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 0);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);

            //World => World
            transformSpace.SetSettingValue("m_targetSpace", VFXCoordinateSpace.Global);
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsFalse(slotVectorExpression.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Local => World
            transformSpace.inputSlots[0].space = VFXCoordinateSpace.Local;
            slotVectorExpression = CollectParentExpression(inlineVector.outputSlots[0].GetExpression()).ToArray();
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
            Assert.IsTrue(slotVectorExpression.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 0);
        }
    }
}
