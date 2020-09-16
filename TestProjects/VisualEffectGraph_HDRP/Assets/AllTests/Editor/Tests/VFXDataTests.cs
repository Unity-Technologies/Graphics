#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
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

        string tempFilePath = "Assets/Temp_vfxTest_Ddata.vfx";

        VFXGraph MakeTemporaryGraph()
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                AssetDatabase.DeleteAsset(tempFilePath);
            }
            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);
            VisualEffectResource resource = asset.GetResource(); // force resource creation
            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();
            graph.visualEffectResource = resource;
            return graph;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            AssetDatabase.DeleteAsset(tempFilePath);
        }

        [Test]
        public void CheckName_Sharing_Between_Output_Event()
        {
            var graph = MakeTemporaryGraph();

            var gameObj = new GameObject("CheckData_Sharing_Between_Output_Event");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var sourceSpawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var eventOutput_A = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var eventOutput_B = ScriptableObject.CreateInstance<VFXOutputEvent>();
            eventOutput_A.LinkFrom(sourceSpawner);
            eventOutput_B.LinkFrom(sourceSpawner);
            Assert.AreEqual(1u, eventOutput_A.inputContexts.Count());
            Assert.AreEqual(1u, eventOutput_B.inputContexts.Count());

            graph.AddChild(sourceSpawner);
            graph.AddChild(eventOutput_A);
            graph.AddChild(eventOutput_B);

            var name_A = eventOutput_A.GetSetting("eventName").value as string;
            var name_B = eventOutput_B.GetSetting("eventName").value as string;

            //Equals names
            Assert.AreEqual(name_A, name_B);
            Assert.AreNotEqual(eventOutput_A.GetData(), eventOutput_B.GetData());

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            var names = new List<string>();
            vfxComponent.GetOutputEventNames(names);
            Assert.AreEqual(1, names.Count);
            Assert.AreEqual(name_A, names[0]);

            var newName = "miaou";
            eventOutput_A.SetSettingValue("eventName", newName);
            name_A = eventOutput_A.GetSetting("eventName").value as string;
            name_B = eventOutput_B.GetSetting("eventName").value as string;
            
            //Now, different names
            Assert.AreNotEqual(name_A, name_B);
            Assert.AreNotEqual(eventOutput_A.GetData(), eventOutput_B.GetData());
            Assert.AreEqual((eventOutput_A.GetData() as VFXDataOutputEvent).eventName, name_A);
            Assert.AreEqual((eventOutput_B.GetData() as VFXDataOutputEvent).eventName, name_B);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            vfxComponent.GetOutputEventNames(names);
            Assert.AreEqual(2, names.Count);
            Assert.IsTrue(names.Contains(newName));
            Assert.AreNotEqual(name_B, newName);
            Assert.IsTrue(names.Contains(name_B));

            //Back to equals names
            eventOutput_B.SetSettingValue("eventName", newName);
            name_A = eventOutput_A.GetSetting("eventName").value as string;
            name_B = eventOutput_B.GetSetting("eventName").value as string;

            Assert.AreEqual(name_A, name_B);
            Assert.AreNotEqual(eventOutput_A.GetData(), eventOutput_B.GetData());
            Assert.AreEqual((eventOutput_A.GetData() as VFXDataOutputEvent).eventName, name_A);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
            vfxComponent.GetOutputEventNames(names);
            Assert.AreEqual(1, names.Count);
            Assert.AreEqual(newName, names[0]);

            UnityEngine.Object.DestroyImmediate(gameObj);
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

        [Test]
        public void CheckCapacityCannotBeZero()
        {
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var data = init.GetData();
            data.SetSettingValue("capacity", 0u);
            uint capacity = (uint)data.GetSettingValue("capacity");
            Assert.NotZero(capacity);
        }

        [Test]
        public void CheckStripCapacityCannotBeZero()
        {
            var init = ScriptableObject.CreateInstance<ContextTestInit>();
            var data = init.GetData();
            data.SetSettingValue("dataType", VFXDataParticle.DataType.ParticleStrip);
            data.SetSettingValue("stripCapacity", 0u);
            data.SetSettingValue("particlePerStripCount", 0u);

            uint capacity = (uint)data.GetSettingValue("capacity");
            uint stripCapacity = (uint)data.GetSettingValue("stripCapacity");
            uint particlePerStripCount = (uint)data.GetSettingValue("particlePerStripCount");

            Assert.NotZero(capacity);
            Assert.NotZero(stripCapacity);
            Assert.NotZero(particlePerStripCount);
        }
    }
}
#endif
