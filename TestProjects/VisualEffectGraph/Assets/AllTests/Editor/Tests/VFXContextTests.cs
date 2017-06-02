using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXContextTests
    {
        private class ContextTestInit : VFXContext
        {
            public ContextTestInit() : base(VFXContextType.kInit) {}
        }

        private class ContextTestUpdate : VFXContext
        {
            public ContextTestUpdate() : base(VFXContextType.kUpdate) {}
        }

        private class ContextTestOutput : VFXContext
        {
            public ContextTestOutput() : base(VFXContextType.kOutput) {}
        }

        private class ContextTestNone : VFXContext
        {
            public ContextTestNone() : base(VFXContextType.kNone) {}
        }

        private class ContextTestInitAndUpdate : VFXContext
        {
            public ContextTestInitAndUpdate() : base(VFXContextType.kInitAndUpdate) {}
        }

        private class ContextTestAll : VFXContext
        {
            public ContextTestAll() : base(VFXContextType.kAll) {}
        }

        private class ContextTestIn : VFXContext
        {
            public ContextTestIn() : base(VFXContextType.kInit, VFXDataType.kNone, VFXDataType.kParticle) {}
        }

        private class ContextTestOut : VFXContext
        {
            public ContextTestOut() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        }

        private void CheckContext(VFXContext context, VFXContextType expectedType)
        {
            Assert.AreEqual(expectedType, context.contextType);
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            CheckContext(ScriptableObject.CreateInstance<ContextTestInit>(), VFXContextType.kInit);
            CheckContext(ScriptableObject.CreateInstance<ContextTestUpdate>(), VFXContextType.kUpdate);
            CheckContext(ScriptableObject.CreateInstance<ContextTestOutput>(), VFXContextType.kOutput);

            CheckContext(ScriptableObject.CreateInstance<ContextTestNone>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kNone");

            CheckContext(ScriptableObject.CreateInstance<ContextTestInitAndUpdate>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kInitAndUpdate");

            CheckContext(ScriptableObject.CreateInstance<ContextTestAll>(), VFXContextType.kNone);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kAll");
        }

        [Test]
        public void Link()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to1 = ScriptableObject.CreateInstance<ContextTestOut>();
            var to2 = ScriptableObject.CreateInstance<ContextTestOut>();

            from.LinkTo(to1);
            to2.LinkFrom(from);

            Assert.AreEqual(0, from.GetNbInputs());
            Assert.AreEqual(1, to1.GetNbInputs());
            Assert.AreEqual(1, to2.GetNbInputs());
            Assert.AreEqual(2, from.GetNbOutputs());
            Assert.AreEqual(0, to1.GetNbOutputs());
            Assert.AreEqual(0, to2.GetNbOutputs());

            Assert.AreEqual(to1, from.GetOutput(0));
            Assert.AreEqual(to2, from.GetOutput(1));
            Assert.AreEqual(from, to1.GetInput(0));
            Assert.AreEqual(from, to2.GetInput(0));
        }

        [Test]
        public void Unlink()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to1 = ScriptableObject.CreateInstance<ContextTestOut>();
            var to2 = ScriptableObject.CreateInstance<ContextTestOut>();

            from.LinkTo(to1);
            to2.LinkFrom(from);

            to1.Unlink(from);
            from.Unlink(to2);

            Assert.AreEqual(0, from.GetNbInputs());
            Assert.AreEqual(0, to1.GetNbInputs());
            Assert.AreEqual(0, to2.GetNbInputs());
            Assert.AreEqual(0, from.GetNbOutputs());
            Assert.AreEqual(0, to1.GetNbOutputs());
            Assert.AreEqual(0, to2.GetNbOutputs());
        }

        [Test]
        public void Link_Fail()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to = ScriptableObject.CreateInstance<ContextTestOut>();

            Assert.Throws<ArgumentException>(() => to.LinkTo(null)); // null
            Assert.Throws<ArgumentException>(() => to.LinkTo(from)); // incompatible types
            Assert.DoesNotThrow(() => from.LinkTo(to));
            Assert.Throws<ArgumentException>(() => from.LinkTo(to)); // double link
        }
    }
}
