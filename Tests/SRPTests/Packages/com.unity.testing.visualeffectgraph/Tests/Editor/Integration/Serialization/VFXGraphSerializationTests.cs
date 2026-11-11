using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Serialization
{
    [TestFixture]
    public class VFXGraphSerializationTests
    {
        private bool m_CreatedTestAssetDirectory;

        [OneTimeSetUp]
        public void SetUp()
        {
            VFXSerializationTestHelper.CreateDirectory(out m_CreatedTestAssetDirectory);
        }

        [Test]
        public void VFXMemorySerializer_Dont_Crash_But_Trigger_Exception_On_Invalid_Usage()
        {
            var vfxGraph = VFXTestCommon.MakeTemporaryGraph();
            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            vfxGraph.AddChild(spawner);

            var dependencies = new HashSet<ScriptableObject>(new [] { vfxGraph });
            vfxGraph.CollectDependencies(dependencies);
            dependencies.Add(null); //Voluntary add an invalid element

            Byte[] backup = null;
            Assert.Throws<NullReferenceException>( () => backup = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray()));
            Assert.IsNull(backup);
        }

        private void CheckIsolatedOperatorAdd(Operator.Add add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(2, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.inputSlots[1].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAdd);
        }

        private void CheckIsolatedOperatorAbs(Operator.Absolute add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(1, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAbs);
        }

        private void CheckConnectedAbs(Operator.Absolute abs)
        {
            Assert.IsTrue(abs.inputSlots[0].HasLink());
            Assert.AreEqual(1, abs.inputSlots[0].LinkedSlots.Count());
            Assert.IsTrue(abs.inputSlots[0].GetExpression() is VFXExpressionAdd);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VisualEffectAsset> write, Action<VisualEffectAsset> read)
        {
            VFXSerializationRoundTripTestHelper.SaveAndReload(VFXSerializationTestHelper.kTestAssetDir, suffixname, write, read);
        }

        private void WriteBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            var add = ScriptableObject.CreateInstance<Operator.Add>();
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetGraph();
            graph.AddChild(add);

            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                var abs = ScriptableObject.CreateInstance<Operator.Absolute>();
                abs.position = new Vector2(64.0f, 64.0f);
                graph.AddChild(abs);
                CheckIsolatedOperatorAbs(abs);
                if (linkAbs)
                {
                    abs.inputSlots[0].Link(add.outputSlots[0]);
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void ReadBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetGraph();
            Assert.AreEqual(spawnAbs ? 2 : 1, graph.GetNbChildren());
            Assert.IsNotNull((Operator.Add)graph[0]);
            var add = (Operator.Add)graph[0];
            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                Assert.IsNotNull((Operator.Absolute)graph[1]);
                var abs = (Operator.Absolute)graph[1];
                CheckIsolatedOperatorAbs(abs);
                Assert.AreEqual(abs.position.x, 64.0f);
                Assert.AreEqual(abs.position.y, 64.0f);
                if (linkAbs)
                {
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void BasicOperatorTest(string suffix, bool spawnAbs, bool linkAbs)
        {
            InnerSaveAndReloadTest(suffix,
                (a) => WriteBasicOperators(a, spawnAbs, linkAbs),
                (a) => ReadBasicOperators(a, spawnAbs, linkAbs));
        }

        [Test]
        public void SerializeOneOperator()
        {
            BasicOperatorTest("One", false, false);
        }

        [Test]
        public void SerializeTwoOperators()
        {
            BasicOperatorTest("Two", true, false);
        }

        [Test]
        public void SerializeTwoOperatorsLink()
        {
            BasicOperatorTest("TwoLinked", true, true);
        }

        [Test]
        public void SerializeOperatorMaskWithState()
        {
            var expectedValue = "xyxy";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var mask = ScriptableObject.CreateInstance<Operator.Swizzle>();
                mask.SetSettingValue("mask", expectedValue);
                asset.GetResource().GetGraph().AddChild(mask);
                Assert.AreEqual(expectedValue, mask.mask);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetGraph();
                Assert.AreEqual(1, graph.GetNbChildren());
                Assert.IsInstanceOf(typeof(Operator.Swizzle), graph[0]);
                var mask = graph[0] as Operator.Swizzle;

                Assert.AreEqual(expectedValue, mask.mask);
            };

            InnerSaveAndReloadTest("Mask", write, read);
        }

        static object[] kCreateAsset_And_Check_Serialized_Instancing_Mode_Cases = Enum.GetValues(typeof(VFXInstancingMode)).Cast<object>().ToArray();

        [Test]
        public void CreateAsset_And_Check_Serialized_Instancing_Mode([ValueSource(nameof(kCreateAsset_And_Check_Serialized_Instancing_Mode_Cases))] object targetMode)
        {
            var mode = (VFXInstancingMode)targetMode;
            var serializedMode = "m_InstancingMode: " + (int)mode;

            var graph = VFXTestCommon.MakeTemporaryGraph();
            var path = AssetDatabase.GetAssetPath(graph);
            graph.GetResource().instancingMode = mode;
            VFXTestCommon.ReimportVFXGraph(graph);
            Assert.AreEqual(mode, graph.GetResource().instancingMode);
            Assert.IsTrue(File.ReadAllText(path).Contains(serializedMode));

            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.ReimportVFXGraph(graph);
            Assert.AreEqual(mode, graph.GetResource().instancingMode);
            Assert.IsTrue(File.ReadAllText(path).Contains(serializedMode));
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            try
            {
                VFXTestCommon.DeleteAllTemporaryGraph();
            }
            finally
            {
                VFXSerializationTestHelper.CleanupDirectory(m_CreatedTestAssetDirectory);
            }
        }
    }

}
