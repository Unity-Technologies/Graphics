using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXDataTests
    {
        private class ContextTestSpawn : VFXContext
        {
            public ContextTestSpawn() : base(VFXContextType.kInit, VFXDataType.kNone, VFXDataType.kSpawnEvent) {}
        }

        private class ContextTestInit : VFXContext
        {
            public ContextTestInit() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        }

        private class ContextTestUpdate : VFXContext
        {
            public ContextTestUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        }

        private class ContextTestOutput : VFXContext
        {
            public ContextTestOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        }

        [Test]
        public void CheckDataType()
        {
            var spawn = ScriptableObject.CreateInstance<ContextTestSpawn>();
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var update = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var output = ScriptableObject.CreateInstance<ContextTestOutput>();

            Assert.IsInstanceOf<VFXDataSpawnEvent>(spawn.GetData());
            Assert.IsInstanceOf<VFXDataParticle>(init.GetData());
            Assert.IsInstanceOf<VFXDataParticle>(update.GetData());
            Assert.IsInstanceOf<VFXDataParticle>(output.GetData());
        }

        [Test]
        public void CheckDataPropagation_Link()
        {
            var spawn = ScriptableObject.CreateInstance<ContextTestSpawn>();
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var update = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var output0 = ScriptableObject.CreateInstance<ContextTestOutput>();
            var output1 = ScriptableObject.CreateInstance<ContextTestOutput>();

            // link in arbitrary order
            update.LinkTo(output0);
            spawn.LinkTo(init);
            update.LinkFrom(init);
            output1.LinkFrom(update);

            var spawnData = spawn.GetData();
            var particleData = init.GetData();

            Assert.IsNotNull(spawnData);
            Assert.IsNotNull(particleData);
            Assert.AreEqual(particleData, update.GetData());
            Assert.AreEqual(particleData, output0.GetData());
            Assert.AreEqual(particleData, output1.GetData());
            Assert.AreNotEqual(particleData, spawnData);
        }

        [Test]
        public void CheckDataPropagation_UnLink()
        {
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var update0 = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var update1 = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var output0 = ScriptableObject.CreateInstance<ContextTestOutput>();
            var output1 = ScriptableObject.CreateInstance<ContextTestOutput>();

            init.LinkTo(update0);
            update0.LinkTo(output0);

            update1.LinkTo(output1);
            init.LinkTo(update1); // this will unlink update0

            init.Unlink(update1);

            var particleData0 = init.GetData();
            var particleData1 = update0.GetData();
            var particleData2 = update1.GetData();

            Assert.IsNotNull(particleData0);
            Assert.IsNotNull(particleData1);
            Assert.IsNotNull(particleData2);

            Assert.AreNotEqual(particleData0, particleData1);
            Assert.AreNotEqual(particleData1, particleData2);
            Assert.AreNotEqual(particleData0, particleData2);

            Assert.AreEqual(particleData1, output0.GetData());
            Assert.AreEqual(particleData2, output1.GetData());
        }
    }
}
