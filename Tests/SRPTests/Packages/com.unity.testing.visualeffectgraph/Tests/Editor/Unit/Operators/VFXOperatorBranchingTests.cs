using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    public class VFXOperatorBranchingTests
    {
        [Test]
        public void BranchOperator_With_Sphere()
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            var sphereA = new Sphere() { center = new Vector3(1.0f, 2.0f, 3.0f), radius = 4.0f };
            var sphereB = new Sphere() { center = new Vector3(1.0f, 2.0f, 3.0f), radius = 4.0f };

            branch.inputSlots[0].value = false;
            branch.inputSlots[1].value = sphereA;
            branch.inputSlots[2].value = sphereB;

            Func<Sphere, Sphere, bool> fnCompareSphere = delegate(Sphere aS, Sphere bS)
            {
                if (aS.center.x != bS.center.x) return false;
                if (aS.center.y != bS.center.y) return false;
                if (aS.center.z != bS.center.z) return false;
                if (aS.radius != bS.radius) return false;
                return true;
            };

            Func<VFXSlot, Sphere> fnSlotToSphere = delegate(VFXSlot slot)
            {
                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var center = context.Compile(slot[0].GetExpression());
                var radius = context.Compile(slot[1].GetExpression());
                return new Sphere()
                {
                    center = center.Get<Vector3>(),
                    radius = radius.Get<float>()
                };
            };

            Assert.IsTrue(fnCompareSphere(fnSlotToSphere(branch.outputSlots[0]), sphereB));

            branch.inputSlots[0].value = true;
            Assert.IsTrue(fnCompareSphere(fnSlotToSphere(branch.outputSlots[0]), sphereA));
        }

        [Test]
        public void BranchOperator_With_Transform()
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetOperandType(typeof(Transform));

            var transformA = new Transform() { position = Vector3.one * 3.0f, angles = Vector3.zero, scale = Vector3.one };
            var transformB = new Transform() { position = Vector3.one * 4.0f, angles = Vector3.zero, scale = Vector3.one };

            branch.inputSlots[0].value = false;
            branch.inputSlots[1].value = transformA;
            branch.inputSlots[2].value = transformB;

            Func<Transform, Transform, bool> fnCompareTransform = delegate(Transform aS, Transform bS)
            {
                //Only compare position => didn't modify something else above
                if (aS.position.x != bS.position.x) return false;
                if (aS.position.y != bS.position.y) return false;
                if (aS.position.z != bS.position.z) return false;
                return true;
            };

            Func<VFXSlot, Transform> fnSlotToTransform = delegate(VFXSlot slot)
            {
                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var position = context.Compile(slot[0].GetExpression());
                var angles = context.Compile(slot[1].GetExpression());
                var scale = context.Compile(slot[2].GetExpression());
                return new Transform()
                {
                    position = position.Get<Vector3>(),
                    angles = angles.Get<Vector3>(),
                    scale = scale.Get<Vector3>(),
                };
            };

            Assert.IsTrue(fnCompareTransform(fnSlotToTransform(branch.outputSlots[0]), transformB));

            branch.inputSlots[0].value = true;
            Assert.IsTrue(fnCompareTransform(fnSlotToTransform(branch.outputSlots[0]), transformA));
        }
    }
}
