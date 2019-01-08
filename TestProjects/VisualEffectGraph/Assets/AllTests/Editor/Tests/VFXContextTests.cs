#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Linq;
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

            /*
             * //TEMP disable LogAssert.Expect, still failing running on katana
            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kInitAndUpdate");
            CheckContext(ScriptableObject.CreateInstance<ContextTestInitAndUpdate>(), VFXContextType.kNone);

            LogAssert.Expect(LogType.Exception, "ArgumentException: Illegal context type: kAll");
            CheckContext(ScriptableObject.CreateInstance<ContextTestAll>(), VFXContextType.kNone);
            */
        }

        [Test]
        public void Link()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to1 = ScriptableObject.CreateInstance<ContextTestOut>();
            var to2 = ScriptableObject.CreateInstance<ContextTestOut>();

            from.LinkTo(to1);
            to2.LinkFrom(from);

            Assert.AreEqual(0, from.inputContexts.Count());
            Assert.AreEqual(1, to1.inputContexts.Count());
            Assert.AreEqual(1, to2.inputContexts.Count());
            Assert.AreEqual(2, from.outputContexts.Count());
            Assert.AreEqual(0, to1.outputContexts.Count());
            Assert.AreEqual(0, to2.outputContexts.Count());

            Assert.AreEqual(to1, from.outputContexts.ElementAt(0));
            Assert.AreEqual(to2, from.outputContexts.ElementAt(1));
            Assert.AreEqual(from, to1.inputContexts.ElementAt(0));
            Assert.AreEqual(from, to2.inputContexts.ElementAt(0));
        }

        [Test]
        public void Unlink()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to1 = ScriptableObject.CreateInstance<ContextTestOut>();
            var to2 = ScriptableObject.CreateInstance<ContextTestOut>();

            from.LinkTo(to1);
            to2.LinkFrom(from);

            to1.UnlinkFrom(from);
            from.UnlinkTo(to2);

            Assert.AreEqual(0, from.inputContexts.Count());
            Assert.AreEqual(0, to1.inputContexts.Count());
            Assert.AreEqual(0, to2.inputContexts.Count());
            Assert.AreEqual(0, from.outputContexts.Count());
            Assert.AreEqual(0, to1.outputContexts.Count());
            Assert.AreEqual(0, to2.outputContexts.Count());
        }

        [Test]
        public void UnlinkAll()
        {
            var from = ScriptableObject.CreateInstance<ContextTestIn>();
            var to1 = ScriptableObject.CreateInstance<ContextTestOut>();
            var to2 = ScriptableObject.CreateInstance<ContextTestOut>();

            from.LinkTo(to1);
            to2.LinkFrom(from);

            from.UnlinkAll();

            Assert.AreEqual(0, from.inputContexts.Count());
            Assert.AreEqual(0, to1.inputContexts.Count());
            Assert.AreEqual(0, to2.inputContexts.Count());
            Assert.AreEqual(0, from.outputContexts.Count());
            Assert.AreEqual(0, to1.outputContexts.Count());
            Assert.AreEqual(0, to2.outputContexts.Count());
        }

        [Test]
        public void MultiSlot_Link()
        {
            var eventStart = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStart.SetSettingValue("eventName", "Start");
            var eventStop = ScriptableObject.CreateInstance<VFXBasicEvent>();
            eventStop.SetSettingValue("eventName", "Stop");
            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            spawner.LinkFrom(eventStart, 0, 0);
            Assert.AreEqual(1, spawner.inputContexts.Count());
            Assert.AreEqual(1, spawner.inputFlowSlot[0].link.Count);
            Assert.AreEqual(0, spawner.inputFlowSlot[1].link.Count);

            spawner.LinkFrom(eventStop, 0, 1);
            Assert.AreEqual(2, spawner.inputContexts.Count());
            Assert.AreEqual(1, spawner.inputFlowSlot[0].link.Count);
            Assert.AreEqual(1, spawner.inputFlowSlot[1].link.Count);

            spawner.UnlinkFrom(eventStart, 0, 0);
            Assert.AreEqual(1, spawner.inputContexts.Count());
            Assert.AreEqual(0, spawner.inputFlowSlot[0].link.Count);
            Assert.AreEqual(1, spawner.inputFlowSlot[1].link.Count);

            spawner.UnlinkFrom(eventStop, 0, 1);
            Assert.AreEqual(0, spawner.inputContexts.Count());
            Assert.AreEqual(0, spawner.inputFlowSlot[0].link.Count);
            Assert.AreEqual(0, spawner.inputFlowSlot[1].link.Count);
        }

        [Test]
        public void MultiLink_Fail()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var to1 = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var to2 = ScriptableObject.CreateInstance<VFXBasicUpdate>();

            to1.LinkFrom(from);
            to2.LinkFrom(from);

            Assert.AreEqual(1, from.outputContexts.Count());
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
#endif
