using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.GraphModel
{
    [TestFixture]
    internal class VFXSlotSpaceTests
    {
        [Test]
        public void SpaceEnumValue_Are_Fixed()
        {
            //The Space Enum are serialized in VFXSlot, we shouldn't modify it without migration solution.
            Assert.AreEqual(-1, (int)VFXSpace.None);
            Assert.AreEqual(0, (int)VFXSpace.Local);
            Assert.AreEqual(1, (int)VFXSpace.World);
        }

        [Test]
        public void Sphere_Type_Should_Be_Spaceable()
        {
            var inline = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inline.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            Assert.IsTrue(inline.inputSlots[0].spaceable);
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

            var allExprCenter = VFXTestCommon.CollectParentExpression(resultCenter).ToArray();
            Assert.IsFalse(allExprCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal)); //everything is within the same space by default

            arcSphere.inputSlots[0].space = VFXSpace.World;

            resultCenter = context.Compile(arcSphere.inputSlots[0][0][0].GetExpression());
            allExprCenter = VFXTestCommon.CollectParentExpression(resultCenter).ToArray();

            Assert.IsTrue(allExprCenter.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        [Test]
        public void SpaceConversion_Sphere_Unexpected_Linking_MasterSlot()
        {
            var sphere_A = ScriptableObject.CreateInstance<VFXInlineOperator>();
            var sphere_B = ScriptableObject.CreateInstance<VFXInlineOperator>();

            sphere_A.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            sphere_B.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            sphere_A.inputSlots[0].space = VFXSpace.World;
            sphere_B.inputSlots[0].space = VFXSpace.Local;

            Assert.AreEqual(VFXSpace.World, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][1].Link(sphere_A.outputSlots[0][1]); //link radius to other radius
            Assert.AreEqual(VFXSpace.Local, sphere_B.outputSlots[0].space);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultCenter = context.Compile(sphere_B.outputSlots[0][0].GetExpression());
            var resultRadius = context.Compile(sphere_B.outputSlots[0][1].GetExpression());

            var allExprCenter = VFXTestCommon.CollectParentExpression(resultCenter).ToArray();
            var allExprRadius = VFXTestCommon.CollectParentExpression(resultRadius).ToArray();

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
            sphere_A.inputSlots[0].space = VFXSpace.World;
            sphere_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            sphere_B.inputSlots[0].space = VFXSpace.Local;

            Assert.AreEqual(VFXSpace.World, sphere_A.outputSlots[0].space);
            Assert.AreEqual(VFXSpace.Local, sphere_B.outputSlots[0].space);

            sphere_B.inputSlots[0][0].Link(sphere_A.outputSlots[0][0]); //link sphere center to other sphere center
            Assert.AreEqual(VFXSpace.Local, sphere_B.outputSlots[0].space);

            var allExprCenter = VFXTestCommon.CollectParentExpression(sphere_B.outputSlots[0][0].GetExpression()).ToArray();
            var allExprRadius = VFXTestCommon.CollectParentExpression(sphere_B.outputSlots[0][1].GetExpression()).ToArray();

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
            position_A.inputSlots[0].space = VFXSpace.World;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXSpace.Local;

            for (int i = 0; i < 3; ++i)
            {
                position_A.outputSlots[0][0][i].Link(position_B.inputSlots[0][0][i]);
            }

            var allExprPosition = VFXTestCommon.CollectParentExpression(position_B.outputSlots[0][0].GetExpression()).ToArray();

            Assert.AreEqual(VFXSpace.World, position_A.outputSlots[0].space);
            Assert.AreEqual(VFXSpace.Local, position_B.outputSlots[0].space);
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
            position_A.inputSlots[0].space = VFXSpace.World;
            position_B.inputSlots[0][0].value = new Vector3(4, 5, 6);
            position_B.inputSlots[0].space = VFXSpace.Local;

            var line = ScriptableObject.CreateInstance<VFXInlineOperator>();
            line.SetSettingValue("m_Type", (SerializableType)typeof(Line));

            line.inputSlots[0].space = VFXSpace.Local;
            line.inputSlots[0][0].Link(position_A.outputSlots[0]);
            line.inputSlots[0][1].Link(position_B.outputSlots[0]);

            var lineOutputSlotA = VFXTestCommon.CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            var lineOutputSlotB = VFXTestCommon.CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();

            Assert.AreEqual(line.inputSlots[0].space, VFXSpace.Local);
            Assert.IsTrue(lineOutputSlotA.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);
            Assert.IsFalse(lineOutputSlotB.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            line.inputSlots[0].space = VFXSpace.World;
            lineOutputSlotA = VFXTestCommon.CollectParentExpression(line.outputSlots[0][0].GetExpression()).ToArray();
            lineOutputSlotB = VFXTestCommon.CollectParentExpression(line.outputSlots[0][1].GetExpression()).ToArray();
            Assert.AreEqual(line.inputSlots[0].space, VFXSpace.World);
            Assert.IsFalse(lineOutputSlotA.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            Assert.IsTrue(lineOutputSlotB.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }
    }
}
