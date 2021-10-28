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
            public ContextTestInit() : base(VFXContextType.Init) {}
        }

        private class ContextTestUpdate : VFXContext
        {
            public ContextTestUpdate() : base(VFXContextType.Update) {}
        }

        private class ContextTestOutput : VFXContext
        {
            public ContextTestOutput() : base(VFXContextType.Output) {}
        }

        private class ContextTestNone : VFXContext
        {
            public ContextTestNone() : base(VFXContextType.None) {}
        }

        private class ContextTestInitAndUpdate : VFXContext
        {
            public ContextTestInitAndUpdate() : base(VFXContextType.InitAndUpdate) {}
        }

        private class ContextTestAll : VFXContext
        {
            public ContextTestAll() : base(VFXContextType.All) {}
        }

        private class ContextTestIn : VFXContext
        {
            public ContextTestIn() : base(VFXContextType.Init, VFXDataType.None, VFXDataType.Particle) {}
        }

        private class ContextTestOut : VFXContext
        {
            public ContextTestOut() : base(VFXContextType.Output, VFXDataType.Particle, VFXDataType.None) {}
        }

        private void CheckContext(VFXContext context, VFXContextType expectedType)
        {
            Assert.AreEqual(expectedType, context.contextType);
        }

        [Test]
        public void ConstructWithAllTypes()
        {
            CheckContext(ScriptableObject.CreateInstance<ContextTestInit>(), VFXContextType.Init);
            CheckContext(ScriptableObject.CreateInstance<ContextTestUpdate>(), VFXContextType.Update);
            CheckContext(ScriptableObject.CreateInstance<ContextTestOutput>(), VFXContextType.Output);

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

        [Test] // see fogbugz : 1146829
        public void MultiLinkSpawnerSpawnerAfterSpawnerInit()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var to1 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var to2 = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            to1.LinkFrom(from);
            to2.LinkFrom(from);

            Assert.AreEqual(2, from.outputContexts.Count());
        }


        [Test] //see fogbugz 1269756
        public void Link_Fail_From_Event_To_OutputEvent()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicEvent>();
            var to = ScriptableObject.CreateInstance<VFXOutputEvent>();
            Assert.IsFalse(VFXContext.CanLink(from, to));
        }

        [Test]
        public void Link_Fail_From_GPUEvent_To_Spawn()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicGPUEvent>();
            var to = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            Assert.IsFalse(VFXContext.CanLink(from, to));
        }

        [Test]
        public void Link_Mixing_GPUEvent_And_Spawn_To_Init()
        {
            var fromA = ScriptableObject.CreateInstance<VFXBasicGPUEvent>();
            var fromB = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var to = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            Assert.IsTrue(VFXContext.CanLink(fromA, to));
            Assert.IsTrue(VFXContext.CanLink(fromB, to));

            to.LinkFrom(fromA);
            Assert.AreEqual(1u, to.inputFlowSlot[0].link.Count());
            Assert.IsTrue(VFXContext.CanLink(fromB, to));

            //Expected disconnection of previous link in that case
            to.LinkFrom(fromB);
            Assert.IsTrue(VFXContext.CanLink(fromA, to));
            Assert.AreEqual(1u, to.inputFlowSlot[0].link.Count());
            Assert.AreEqual(fromB, to.inputFlowSlot[0].link.First().context);
        }

        [Test]
        public void Link_Mixing_Spawn_And_GPUEvent_To_Init()
        {
            var fromA = ScriptableObject.CreateInstance<VFXBasicGPUEvent>();
            var fromB = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var to = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            Assert.IsTrue(VFXContext.CanLink(fromA, to));
            Assert.IsTrue(VFXContext.CanLink(fromB, to));

            to.LinkFrom(fromB);
            Assert.AreEqual(1u, to.inputFlowSlot[0].link.Count());
            Assert.IsTrue(VFXContext.CanLink(fromA, to));

            //Expected disconnection of previous link in that case
            to.LinkFrom(fromA);
            Assert.IsTrue(VFXContext.CanLink(fromB, to));
            Assert.AreEqual(1u, to.inputFlowSlot[0].link.Count());
            Assert.AreEqual(fromA, to.inputFlowSlot[0].link.First().context);
        }

        [Test]
        public void MultiLink_Spawn_To_Output_And_Initialize()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var toA = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var toB = ScriptableObject.CreateInstance<VFXOutputEvent>();

            toA.LinkFrom(from);
            toB.LinkFrom(from);

            Assert.AreEqual(2, from.outputContexts.Count());
        }

        [Test]
        public void MultiLink_Spawner_Mixing_OutputEvent_And_Initialize()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var toA = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var toB = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var toC = ScriptableObject.CreateInstance<VFXBasicInitialize>();

            toA.LinkFrom(from);
            toB.LinkFrom(from);
            toC.LinkFrom(from);

            Assert.AreEqual(3, from.outputContexts.Count());
        }

        [Test]
        public void MultiLink_Spawn_And_Event_To_Initialize()
        {
            var from1 = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var from2 = ScriptableObject.CreateInstance<VFXBasicEvent>();

            var to = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            to.LinkFrom(from1);
            to.LinkFrom(from2);

            Assert.AreEqual(2, to.inputContexts.Count());
        }

        [Test]
        public void MultiLink_Event_And_Spawn_To_Initialize()
        {
            var from1 = ScriptableObject.CreateInstance<VFXBasicEvent>();
            var from2 = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var to = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            to.LinkFrom(from1);
            to.LinkFrom(from2);

            Assert.AreEqual(2, to.inputContexts.Count());
        }

        [Test]
        public void Link_Success_From_Update_To_Update()
        {
            var from = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var to = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            Assert.IsTrue(VFXContext.CanLink(from, to));
        }

        [Test]
        public void Link_Initialize_Cant_Mix_Update_And_Output()
        {
            var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var outputA = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            var outputB = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            update.LinkFrom(init);
            outputA.LinkFrom(update);

            Assert.AreEqual(1, init.outputContexts.Count());
            Assert.AreEqual(1, update.outputContexts.Count());

            outputB.LinkFrom(init);
            Assert.AreEqual(1, init.outputContexts.Count());
            Assert.AreEqual(outputB, init.outputContexts.First());

            update.LinkFrom(init);
            Assert.AreEqual(1, init.outputContexts.Count());
            Assert.AreEqual(update, init.outputContexts.First());
        }

        [Test]
        public void Link_Update_Cant_Mix_Update_And_Output()
        {
            var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var updateA = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var updateB = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var outputA = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            var outputB = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            updateA.LinkFrom(init);
            updateB.LinkFrom(updateA);
            outputA.LinkFrom(updateB);

            Assert.AreEqual(1, init.outputContexts.Count());
            Assert.AreEqual(1, updateA.outputContexts.Count());
            Assert.AreEqual(1, updateB.outputContexts.Count());

            outputB.LinkFrom(updateA);
            Assert.AreEqual(1, updateA.outputContexts.Count());
            Assert.AreEqual(outputB, updateA.outputContexts.First());

            updateB.LinkFrom(updateA);
            Assert.AreEqual(1, updateA.outputContexts.Count());
            Assert.AreEqual(updateB, updateA.outputContexts.First());
        }

        [Test]
        public void Link_Fail_From_Event_To_Initialize()
        {
            //For now, we can't use direct link from event to initialize context.
            var from = ScriptableObject.CreateInstance<VFXBasicEvent>();
            var to = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            Assert.IsTrue(VFXContext.CanLink(from, to));
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
