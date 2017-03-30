using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSlotTests
    {
        class VFXDummySlot : VFXSlot
        {

        }

        [Test]
        public void Link()
        {
            VFXSlot input = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            input.Link(output);

            Assert.AreEqual(1, input.GetNbLinks());
            Assert.AreEqual(1, output.GetNbLinks());
            Assert.AreEqual(output, input.refSlot);
            Assert.AreEqual(output, output.refSlot);
        }

        [Test]
        public void Unlink()
        {
            VFXSlot input = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            input.Link(output);
            input.Unlink(output);

            Assert.AreEqual(0, input.GetNbLinks());
            Assert.AreEqual(0, output.GetNbLinks());
            Assert.AreEqual(input, input.refSlot);
            Assert.AreEqual(output, output.refSlot);
        }

        [Test]
        public void Link_Multiple()
        {
            VFXSlot input0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot input1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);
            VFXSlot output1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);

            output0.Link(input0);
            output0.Link(input1);

            Assert.AreEqual(2, output0.GetNbLinks());

            output1.Link(input0);

            Assert.AreEqual(1, input0.GetNbLinks());
            Assert.AreEqual(1, input1.GetNbLinks());
            Assert.AreEqual(1, output0.GetNbLinks());
            Assert.AreEqual(1, output1.GetNbLinks());
            Assert.AreEqual(output1, input0.refSlot);
            Assert.AreEqual(output0, input1.refSlot);
        }

        [Test]
        public void UnlinkAll()
        {
            const int NB_INPUTS = 10;

            VFXSlot output = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kOutput);
            for (int i = 0; i < NB_INPUTS; ++i)
                output.Link(VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput));

            Assert.AreEqual(NB_INPUTS, output.GetNbLinks());

            output.UnlinkAll();

            Assert.AreEqual(0, output.GetNbLinks());
        }

        [Test]
        public void Link_Fail()
        {
            VFXSlot input0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot input1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);

            VFXSlot output0 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);
            VFXSlot output1 = VFXSlot.Create(new VFXProperty(typeof(float), "test"), VFXSlot.Direction.kInput);

            input0.Link(input1);
            output0.Link(output1);

            Assert.AreEqual(0, input0.GetNbLinks());
            Assert.AreEqual(0, input1.GetNbLinks());
            Assert.AreEqual(0, output0.GetNbLinks());
            Assert.AreEqual(0, output1.GetNbLinks());
        }

        private void CheckVectorSlotCreation(Type type, VFXSlot.Direction direction, int expectionChildrenNb)
        {
            VFXSlot slot = VFXSlot.Create(new VFXProperty(type, "test"), direction);

            Assert.IsNotNull(slot);
            Assert.AreEqual(expectionChildrenNb, slot.GetNbChildren());
            Assert.IsInstanceOf<VFXExpressionCombine>(slot.expression);

            foreach (var child in slot.children)
            {
                Assert.IsNotNull(child);
                Assert.AreEqual(0, child.GetNbChildren());
                Assert.IsInstanceOf<VFXExpressionExtractComponent>(child.expression);
            }
        }

        [Test]
        public void Create()
        {
            CheckVectorSlotCreation(typeof(Vector2), VFXSlot.Direction.kInput, 2);
            CheckVectorSlotCreation(typeof(Vector3), VFXSlot.Direction.kInput, 3);
            CheckVectorSlotCreation(typeof(Vector4), VFXSlot.Direction.kInput, 4);

            CheckVectorSlotCreation(typeof(Vector2), VFXSlot.Direction.kOutput, 2);
            CheckVectorSlotCreation(typeof(Vector3), VFXSlot.Direction.kOutput, 3);
            CheckVectorSlotCreation(typeof(Vector4), VFXSlot.Direction.kOutput, 4);
        }

        [Test]
        public void CheckExpression()
        {
            VFXSlot sphereSlot = VFXSlot.Create(new VFXProperty(typeof(Sphere), "sphere"), VFXSlot.Direction.kInput);
            VFXSlot floatSlot = VFXSlot.Create(new VFXProperty(typeof(float), "float"), VFXSlot.Direction.kOutput);

            sphereSlot.GetChild(0).GetChild(0).Link(floatSlot);
            sphereSlot.GetChild(1).Link(floatSlot);

            var expr = sphereSlot.GetChild(0).GetChild(0).expression;
            Assert.IsInstanceOf<VFXExpressionExtractComponent>(expr);
            Assert.AreEqual(floatSlot.expression, expr.Parents[0].Parents[0]);
            Assert.AreEqual(floatSlot.expression, sphereSlot.GetChild(1).expression);

            floatSlot.UnlinkAll();
            expr = sphereSlot.GetChild(0).GetChild(0).expression;
            Assert.IsInstanceOf<VFXExpressionExtractComponent>(expr);
            Assert.AreNotEqual(floatSlot.expression, expr.Parents[0].Parents[0]);
            Assert.AreNotEqual(floatSlot.expression, sphereSlot.GetChild(1).expression);
        }
    }
}
