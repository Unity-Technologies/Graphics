using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSlotTests
    {
        [Test]
        public void Link()
        {
            VFXSlot input = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot output = new VFXSlot(VFXSlot.Direction.kOutput);

            input.Link(output);

            Assert.AreEqual(1, input.GetNbLinks());
            Assert.AreEqual(1, output.GetNbLinks());
            Assert.AreEqual(output, input.refSlot);
            Assert.AreEqual(output, output.refSlot);
        }

        [Test]
        public void Unlink()
        {
            VFXSlot input = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot output = new VFXSlot(VFXSlot.Direction.kOutput);

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
            VFXSlot input0 = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot input1 = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot output0 = new VFXSlot(VFXSlot.Direction.kOutput);
            VFXSlot output1 = new VFXSlot(VFXSlot.Direction.kOutput);

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

            VFXSlot output = new VFXSlot(VFXSlot.Direction.kOutput);
            for (int i = 0; i < NB_INPUTS; ++i)
                output.Link(new VFXSlot(VFXSlot.Direction.kInput));

            Assert.AreEqual(NB_INPUTS, output.GetNbLinks());

            output.UnlinkAll();

            Assert.AreEqual(0, output.GetNbLinks());
        }

        [Test]
        public void Link_Fail()
        {
            VFXSlot input0 = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot input1 = new VFXSlot(VFXSlot.Direction.kInput);

            VFXSlot output0 = new VFXSlot(VFXSlot.Direction.kInput);
            VFXSlot output1 = new VFXSlot(VFXSlot.Direction.kInput);

            input0.Link(input1);
            output0.Link(output1);

            Assert.AreEqual(0, input0.GetNbLinks());
            Assert.AreEqual(0, input1.GetNbLinks());
            Assert.AreEqual(0, output0.GetNbLinks());
            Assert.AreEqual(0, output1.GetNbLinks());
        }
    }
}
