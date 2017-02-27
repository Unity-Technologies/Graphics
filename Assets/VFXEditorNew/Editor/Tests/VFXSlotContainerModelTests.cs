using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSlotContainerModelTests
    {
        private class TestSlotContainer : VFXSlotContainerModel<VFXModel, VFXModel>
        {
            public class InputProperties
            {
                public Vector4 v;
                public float f;
            }

            public class OutputProperties
            {
                public Vector2 v2;
                public Vector3 v3;
            }
        }

        private void CheckSlotEnumerable(TestSlotContainer model,IEnumerable<VFXSlot> slots, VFXProperty[] correctProperties)
        {
            // First count slots
            int index = 0;
            foreach (var slot in slots)
                ++index;
            Assert.AreEqual(correctProperties.Length, index);

            index = 0;
            foreach (var slot in slots)
            {
                Assert.AreEqual(correctProperties[index].name,slot.name);
                Assert.AreEqual(correctProperties[index], slot.property);
                Assert.AreEqual(model, slot.owner);
                ++index;
            }
        }

        [Test]
        public void GetSlots()
        {
            var correctInputs = new VFXProperty[2]  {
                new VFXProperty(typeof(Vector4),"v"),  
                new VFXProperty(typeof(float),"f")
            };

            var correctOutputs = new VFXProperty[2] {
                new VFXProperty(typeof(Vector2),"v2"), 
                new VFXProperty(typeof(Vector3),"v3")
            };

            var model = new TestSlotContainer();
            Assert.AreEqual(correctInputs.Length, model.GetNbInputSlots());
            Assert.AreEqual(correctOutputs.Length, model.GetNbOutputSlots());

            CheckSlotEnumerable(model, model.inputSlots, correctInputs);
            CheckSlotEnumerable(model, model.outputSlots, correctOutputs);
        }

        [Test]
        public void AddSlot()
        {
            var model = new TestSlotContainer();

            var inputSlot = VFXSlot.Create(new VFXProperty(typeof(Texture2D),"t"), VFXSlot.Direction.kInput);
            Assert.IsNull(inputSlot.owner);
            model.AddSlot(inputSlot);
            Assert.AreEqual(model, inputSlot.owner);
            Assert.AreEqual(3, model.GetNbInputSlots());
            Assert.AreEqual(inputSlot, model.GetInputSlot(2));

            var outputSlot = VFXSlot.Create(new VFXProperty(typeof(Texture2D), "t"), VFXSlot.Direction.kOutput);
            Assert.IsNull(outputSlot.owner);
            model.AddSlot(outputSlot);
            Assert.AreEqual(model, outputSlot.owner);
            Assert.AreEqual(3, model.GetNbOutputSlots());
            Assert.AreEqual(outputSlot, model.GetOutputSlot(2));
        }

        [Test]
        public void RemoveSlot()
        {
            var model = new TestSlotContainer();

            var inputSlot = model.GetInputSlot(0);
            model.RemoveSlot(inputSlot);
            Assert.IsNull(inputSlot.owner);
            Assert.AreEqual(1, model.GetNbInputSlots());

            var outputSlot = model.GetOutputSlot(1);
            model.RemoveSlot(outputSlot);
            Assert.IsNull(outputSlot.owner);
            Assert.AreEqual(1, model.GetNbOutputSlots());     
        }
    }
}