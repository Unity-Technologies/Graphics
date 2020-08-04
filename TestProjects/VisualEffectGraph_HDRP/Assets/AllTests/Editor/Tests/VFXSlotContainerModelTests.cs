#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSlotContainerModelTests
    {
        private class TestSlotContainer : VFXSlotContainerModel<VFXModel, VFXModel>
        {
            public class InputProperties
            {
                public Vector4 v = Vector4.zero;
                public float f = 0;
            }

            public class OutputProperties
            {
                public Vector2 v2 = Vector2.zero;
                public Vector3 v3 = Vector2.zero;
            }
        }

        private class DynamicSlotContainer : VFXSlotContainerModel<VFXModel, VFXModel>
        {
            [VFXSetting]
            public int slotSetting = 0;

            public class InputProperties1
            {
                public float f = 1.0f;
            }

            public class InputProperties2
            {
                public Vector3 v = Vector3.one;
                public float f = 2.0f;
            }

            public class InputProperties3
            {
                [ShowAsColor]
                public Vector3 v = Vector3.one;
                public float f = 2.0f;
            }

            private static IEnumerable<VFXPropertyWithValue> ProceduralProperties()
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector3), "v"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "v2"));
            }

            protected override IEnumerable<VFXPropertyWithValue> inputProperties
            {
                get
                {
                    switch (slotSetting)
                    {
                        case 0:     return PropertiesFromType("InputProperties1");
                        case 1:     return PropertiesFromType("InputProperties2");
                        case 2:     return PropertiesFromType("InputProperties1").Concat(PropertiesFromType("InputProperties2"));
                        case 3:     return ProceduralProperties();
                        case 5:     return PropertiesFromType("InputProperties3");
                        default:    return PropertiesFromSlots(inputSlots);
                    }
                }
            }
        }

        private void CheckSlotEnumerable(TestSlotContainer model, IEnumerable<VFXSlot> slots, VFXProperty[] correctProperties)
        {
            // First count slots
            int index = 0;
            foreach (var slot in slots)
                ++index;
            Assert.AreEqual(correctProperties.Length, index);

            index = 0;
            foreach (var slot in slots)
            {
                Assert.AreEqual(correctProperties[index].name, slot.name);
                Assert.AreEqual(correctProperties[index], slot.property);
                Assert.AreEqual(model, slot.owner);
                ++index;
            }
        }

        [Test]
        public void GetSlots()
        {
            var correctInputs = new VFXProperty[2]
            {
                new VFXProperty(typeof(Vector4), "v"),
                new VFXProperty(typeof(float), "f")
            };

            var correctOutputs = new VFXProperty[2]
            {
                new VFXProperty(typeof(Vector2), "v2"),
                new VFXProperty(typeof(Vector3), "v3")
            };

            var model = ScriptableObject.CreateInstance<TestSlotContainer>();
            Assert.AreEqual(correctInputs.Length, model.GetNbInputSlots());
            Assert.AreEqual(correctOutputs.Length, model.GetNbOutputSlots());

            CheckSlotEnumerable(model, model.inputSlots, correctInputs);
            CheckSlotEnumerable(model, model.outputSlots, correctOutputs);
        }

        [Test]
        public void AddSlot()
        {
            var model = ScriptableObject.CreateInstance<TestSlotContainer>();

            var inputSlot = VFXSlot.Create(new VFXProperty(typeof(Texture2D), "t"), VFXSlot.Direction.kInput);
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
            var model = ScriptableObject.CreateInstance<TestSlotContainer>();

            var inputSlot = model.GetInputSlot(0);
            model.RemoveSlot(inputSlot);
            Assert.IsNull(inputSlot.owner);
            Assert.AreEqual(1, model.GetNbInputSlots());

            var outputSlot = model.GetOutputSlot(1);
            model.RemoveSlot(outputSlot);
            Assert.IsNull(outputSlot.owner);
            Assert.AreEqual(1, model.GetNbOutputSlots());
        }

        [Test]
        public void DynamicSlots()
        {
            var model = ScriptableObject.CreateInstance<DynamicSlotContainer>();

            Assert.AreEqual(1, model.GetNbInputSlots());
            Assert.AreEqual(new VFXProperty(typeof(float), "f"), model.GetInputSlot(0).property);
            Assert.AreEqual(1.0f, model.GetInputSlot(0).value);

            model.SetSettingValue("slotSetting", 1);
            Assert.AreEqual(2, model.GetNbInputSlots());
            Assert.AreEqual(new VFXProperty(typeof(Vector3), "v"), model.GetInputSlot(0).property);
            Assert.AreEqual(Vector3.one, model.GetInputSlot(0).value);
            Assert.AreEqual(new VFXProperty(typeof(float), "f"), model.GetInputSlot(1).property);
            Assert.AreEqual(1.0f, model.GetInputSlot(1).value); // Must have conserve the value from previous slot

            model.SetSettingValue("slotSetting", 2);
            Assert.AreEqual(3, model.GetNbInputSlots());
            Assert.AreEqual(new VFXProperty(typeof(float), "f"), model.GetInputSlot(0).property);
            Assert.AreEqual(1.0f, model.GetInputSlot(0).value);
            Assert.AreEqual(new VFXProperty(typeof(Vector3), "v"), model.GetInputSlot(1).property);
            Assert.AreEqual(Vector3.one, model.GetInputSlot(1).value);
            Assert.AreEqual(new VFXProperty(typeof(float), "f"), model.GetInputSlot(2).property);
            Assert.AreEqual(2.0f, model.GetInputSlot(2).value);

            var outputSlot = VFXSlot.Create(new VFXProperty(typeof(Vector3), "o"), VFXSlot.Direction.kOutput);
            model.GetInputSlot(1).Link(outputSlot);
            model.GetInputSlot(1).value = new Vector3(1.0f, 2.0f, 3.0f);

            model.SetSettingValue("slotSetting", 3);
            Assert.AreEqual(2, model.GetNbInputSlots());
            Assert.AreEqual(new VFXProperty(typeof(Vector3), "v"), model.GetInputSlot(0).property);
            Assert.AreEqual(new Vector3(1.0f, 2.0f, 3.0f), model.GetInputSlot(0).value);
            Assert.AreEqual(1, model.GetInputSlot(0).GetNbLinks());
            Assert.AreEqual(new VFXProperty(typeof(Vector2), "v2"), model.GetInputSlot(1).property);
            Assert.AreEqual(Vector2.zero, model.GetInputSlot(1).value);

            model.SetSettingValue("slotSetting", 4);
            Assert.AreEqual(2, model.GetNbInputSlots());
            Assert.AreEqual(new VFXProperty(typeof(Vector3), "v"), model.GetInputSlot(0).property);
            Assert.AreEqual(new Vector3(1.0f, 2.0f, 3.0f), model.GetInputSlot(0).value);
            Assert.AreEqual(1, model.GetInputSlot(0).GetNbLinks());
            Assert.AreEqual(new VFXProperty(typeof(Vector2), "v2"), model.GetInputSlot(1).property);
            Assert.AreEqual(Vector2.zero, model.GetInputSlot(1).value);


            model.SetSettingValue("slotSetting", 1);
            Assert.IsTrue(model.GetInputSlot(0).property.attributes.Length == 0);

            model.SetSettingValue("slotSetting", 5);
            Assert.IsTrue(model.GetInputSlot(0).property.attributes.Length == 1);
        }

        [Test]
        public void ImplicitExpressionTransferWithCompoundType()
        {
            var outputSlot = VFXSlot.Create(new VFXProperty(typeof(ArcSphere), "o"), VFXSlot.Direction.kOutput);
            var inputSlot = VFXSlot.Create(new VFXProperty(typeof(ArcSphere), "i"), VFXSlot.Direction.kInput);

            var radius = 123.0f;
            outputSlot.children.FirstOrDefault(o => o.name == "sphere").children.FirstOrDefault(o => o.name == "radius").value = radius;
            inputSlot.Link(outputSlot);
            Assert.AreEqual(radius, inputSlot.children.FirstOrDefault(o => o.name == "sphere").children.FirstOrDefault(o => o.name == "radius").GetExpression().Get<float>());
        }
    }
}
#endif
