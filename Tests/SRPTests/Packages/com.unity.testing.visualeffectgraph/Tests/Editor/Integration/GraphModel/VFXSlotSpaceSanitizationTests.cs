using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.GraphModel
{
    [TestFixture]
    internal class VFXSlotSpaceSanitizationTests
    {
        [Test]
        public void Space_Slot_Sanitize_Still_Possible_Simple_Sphere()
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));

            var slot = branch.inputSlots[1];
            Assert.AreEqual(typeof(Sphere), slot.property.type);

            slot.space = VFXSpace.World;
            Assert.AreEqual(VFXSpace.World, slot.space);
            slot.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            slot.Sanitize(-1);
            Assert.AreEqual(VFXSpace.World, branch.inputSlots[1].space);
        }

        #pragma warning disable 0414
        private static bool[] trueOrFalse = { true, false };
        #pragma warning restore 0414

        [Test]
        public void Space_Slot_Sanitize_Still_Possible_ArcSphere([ValueSource("trueOrFalse")] bool fromParentToChildSanitize, [ValueSource("trueOrFalse")] bool hackChildSphere)
        {
            var branch = ScriptableObject.CreateInstance<Operator.Branch>();
            branch.SetSettingValue("m_Type", (SerializableType)typeof(ArcSphere));

            var slot = branch.inputSlots[1];
            Assert.AreEqual(typeof(ArcSphere), slot.property.type);

            slot.space = VFXSpace.World;
            Assert.AreEqual(VFXSpace.World, slot.space);

            var slotToHack = hackChildSphere ? slot : slot.children.First();
            slotToHack.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            if (fromParentToChildSanitize)
            {
                slot.Sanitize(-1);
                slot.children.First().Sanitize(-1);
            }
            else
            {
                slot.children.First().Sanitize(-1);
                slot.Sanitize(-1);
            }
            Assert.AreEqual(VFXSpace.World, branch.inputSlots[1].space);
        }

        [Test]
        public void Space_Slot_Sanitize_Still_Possible_Even_With_Linked_Slot([ValueSource("trueOrFalse")] bool reverseSanitizeOrdering, [ValueSource("trueOrFalse")] bool hackArcSphere, [ValueSource("trueOrFalse")] bool hackSphere)
        {
            var inlineOpSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineOpSphere.SetSettingValue("m_Type", (SerializableType)typeof(Sphere));
            inlineOpSphere.inputSlots[0].space = VFXSpace.World;

            var inlineOpArcSphere = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineOpArcSphere.SetSettingValue("m_Type", (SerializableType)typeof(ArcSphere));
            inlineOpArcSphere.inputSlots[0].space = VFXSpace.Local;

            inlineOpSphere.outputSlots[0].Link(inlineOpArcSphere.inputSlots[0][0]);
            Assert.IsTrue(inlineOpSphere.outputSlots[0].HasLink());
            {
                var allExpr = VFXTestCommon.CollectParentExpression(inlineOpArcSphere.outputSlots[0][0][0].GetExpression()).ToArray();
                Assert.IsTrue(allExpr.Count(o =>
                {
                    return o.operation == VFXExpressionOperation.WorldToLocal;
                }) == 1);
            }

            var objs = new HashSet<ScriptableObject>();
            inlineOpSphere.CollectDependencies(objs);
            inlineOpArcSphere.CollectDependencies(objs);

            //Hacking type to simulation a change of type description
            var allSlot = objs.OfType<VFXSlot>();
            var hackedSlot = Enumerable.Empty<VFXSlot>();
            if (hackArcSphere)
            {
                hackedSlot = hackedSlot.Concat(allSlot.Where(o => o.property.type == typeof(ArcSphere)));
            }

            if (hackSphere)
            {
                hackedSlot = hackedSlot.Concat(allSlot.Where(o => o.property.type == typeof(Sphere)));
            }

            foreach (var slotToHack in hackedSlot)
            {
                slotToHack.AddChild(VFXSlot.Create(new VFXProperty(typeof(float), "hacked"), VFXSlot.Direction.kInput), -1, false);
            }

            //Apply Sanitize
            var objsEnumerable = objs.AsEnumerable<ScriptableObject>();
            if (reverseSanitizeOrdering)
            {
                objsEnumerable = objsEnumerable.Reverse();
            }

            foreach (var obj in objsEnumerable.OfType<VFXModel>())
            {
                obj.Sanitize(-1);
            }

            if (!hackArcSphere)
            {
                Assert.IsTrue(inlineOpSphere.outputSlots[0].HasLink());
            } // else, expected disconnection, parent has changed (log a message like this " didnt match the type layout. It is recreated and all links are lost.")

            if (inlineOpSphere.outputSlots[0].HasLink())
            {
                var allExpr = VFXTestCommon.CollectParentExpression(inlineOpArcSphere.outputSlots[0][0][0].GetExpression()).ToArray();
                Assert.IsTrue(allExpr.Count(o =>
                {
                    return o.operation == VFXExpressionOperation.WorldToLocal;
                }) == 1);
            }
        }
    }
}
