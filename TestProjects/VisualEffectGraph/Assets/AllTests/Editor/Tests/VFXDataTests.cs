#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXDataTests
    {
        private static VFXAttribute attrib1 = new VFXAttribute("attrib1", VFXValueType.Float);
        private static VFXAttribute attrib2 = new VFXAttribute("attrib2", VFXValueType.Float2);
        private static VFXAttribute attrib3 = new VFXAttribute("attrib3", VFXValueType.Float3);
        private static VFXAttribute attrib4 = new VFXAttribute("attrib4", VFXValueType.Float4);

        private class ContextTestSpawn : VFXContext
        {
            public ContextTestSpawn() : base(VFXContextType.Init, VFXDataType.None, VFXDataType.SpawnEvent) {}
        }

        private class ContextTestInit : VFXContext
        {
            public ContextTestInit() : base(VFXContextType.Init, VFXDataType.SpawnEvent, VFXDataType.Particle) {}
            public override IEnumerable<VFXAttributeInfo> attributes
            {
                get
                {
                    yield return new VFXAttributeInfo(attrib2, VFXAttributeMode.Write);
                    yield return new VFXAttributeInfo(attrib3, VFXAttributeMode.Read);
                }
            }
        }

        private class ContextTestUpdate : VFXContext
        {
            public ContextTestUpdate() : base(VFXContextType.Update, VFXDataType.Particle, VFXDataType.Particle) {}
            public override IEnumerable<VFXAttributeInfo> attributes
            {
                get
                {
                    yield return new VFXAttributeInfo(attrib1, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(attrib3, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(attrib4, VFXAttributeMode.Write);
                }
            }
        }

        private class ContextTestOutput : VFXContext
        {
            public ContextTestOutput() : base(VFXContextType.Output, VFXDataType.Particle, VFXDataType.None) {}
            public override IEnumerable<VFXAttributeInfo> attributes
            {
                get
                {
                    yield return new VFXAttributeInfo(attrib2, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(attrib3, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(attrib4, VFXAttributeMode.Write);
                }
            }
        }

        [Test]
        public void CheckDataType()
        {
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var update = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var output = ScriptableObject.CreateInstance<ContextTestOutput>();

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

            Assert.IsNull(spawnData);
            Assert.IsNotNull(particleData);
            Assert.AreEqual(particleData, update.GetData());
            Assert.AreEqual(particleData, output0.GetData());
            Assert.AreEqual(particleData, output1.GetData());
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
            init.UnlinkTo(update1);

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

        [Test]
        public void CheckAttributes()
        {
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var update = ScriptableObject.CreateInstance<ContextTestUpdate>();
            var output = ScriptableObject.CreateInstance<ContextTestOutput>();

            init.LinkTo(update);
            update.LinkTo(output);

            VFXData data = init.GetData();
            data.CollectAttributes();

            Assert.AreEqual(4, data.GetNbAttributes());

            Assert.IsTrue(data.IsAttributeStored(attrib1));
            Assert.IsTrue(data.IsAttributeStored(attrib2));
            Assert.IsTrue(data.IsAttributeLocal(attrib3));
            Assert.IsTrue(data.IsAttributeLocal(attrib4));

            Assert.IsTrue(data.IsCurrentAttributeRead(attrib1));
            Assert.IsTrue(data.IsCurrentAttributeRead(attrib2));
            Assert.IsTrue(data.IsCurrentAttributeRead(attrib3));
            Assert.IsFalse(data.IsCurrentAttributeRead(attrib4));

            Assert.IsTrue(data.IsCurrentAttributeWritten(attrib1));
            Assert.IsTrue(data.IsCurrentAttributeWritten(attrib2));
            Assert.IsFalse(data.IsCurrentAttributeWritten(attrib3));
            Assert.IsTrue(data.IsCurrentAttributeWritten(attrib4));
        }
    }
}
#endif
