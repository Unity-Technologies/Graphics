using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSerializationTests
    {
        private readonly static string kTestAssetDir = "Assets/VFXEditorNew/Editor/Tests";
        private readonly static string kTestAssetName = "TestAsset";
        private readonly static string kTestAssetPath = kTestAssetDir + "/" + kTestAssetName + ".asset";

        [OneTimeSetUpAttribute]
        public void OneTimeSetUpAttribute()
        {
            string[] guids = AssetDatabase.FindAssets(kTestAssetName, new string[] { kTestAssetDir });

            // If the asset does not exist, create it
            if (guids.Length == 0)
            {
                VFXGraphAsset asset = ScriptableObject.CreateInstance<VFXGraphAsset>();
                InitAsset(asset);
                AssetDatabase.CreateAsset(asset,kTestAssetPath);
				asset.UpdateSubAssets();
            }
        }

        [Test]
        public void SerializeModel()
        {
            VFXGraphAsset assetSrc = ScriptableObject.CreateInstance<VFXGraphAsset>();
            VFXGraphAsset assetDst = ScriptableObject.CreateInstance<VFXGraphAsset>();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }

        [Test]
        public void LoadAssetFromPath()
        {
            VFXGraphAsset asset = AssetDatabase.LoadAssetAtPath<VFXGraphAsset>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void InitAsset(VFXGraphAsset asset)
        {
            asset.root.RemoveAllChildren();

            VFXSystem system0 = new VFXSystem();
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicUpdate>());
            system0.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());

            VFXSystem system1 = new VFXSystem();
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicInitialize>());
            system1.AddChild(ScriptableObject.CreateInstance<VFXBasicOutput>());

            // Add some block
            var block0 = ScriptableObject.CreateInstance<VFXInitBlockTest>();
            var block1 = ScriptableObject.CreateInstance<VFXUpdateBlockTest>();
            var block2 = ScriptableObject.CreateInstance<VFXOutputBlockTest>();

            // Add some operator
            VFXOperator add = ScriptableObject.CreateInstance<VFXOperatorAdd>();

            system0[0].AddChild(block0);
            system0[1].AddChild(block1);
            system0[2].AddChild(block2);

            asset.root.AddChild(system0);
            asset.root.AddChild(system1);
            asset.root.AddChild(add);
        }

        private void CheckAsset(VFXGraphAsset asset)
        {
            Assert.AreEqual(3, asset.root.GetNbChildren());

            Assert.AreEqual(3, asset.root[0].GetNbChildren());
            Assert.AreEqual(2, asset.root[1].GetNbChildren());
            Assert.AreEqual(0, asset.root[2].GetNbChildren());

            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(1));
            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(2));
            Assert.IsNotNull(((VFXSystem)(asset.root[1])).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(asset.root[1])).GetChild(1));

            Assert.AreEqual(VFXContextType.kInit, ((VFXSystem)(asset.root[0])).GetChild(0).contextType);
            Assert.AreEqual(VFXContextType.kUpdate, ((VFXSystem)(asset.root[0])).GetChild(1).contextType);
            Assert.AreEqual(VFXContextType.kOutput, ((VFXSystem)(asset.root[0])).GetChild(2).contextType);
            Assert.AreEqual(VFXContextType.kInit, ((VFXSystem)(asset.root[1])).GetChild(0).contextType);
            Assert.AreEqual(VFXContextType.kOutput, ((VFXSystem)(asset.root[1])).GetChild(1).contextType);

            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(0).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(1).GetChild(0));
            Assert.IsNotNull(((VFXSystem)(asset.root[0])).GetChild(2).GetChild(0));
            Assert.IsNotNull((VFXOperatorAdd)asset.root[2]);
        }

        private void CheckIsolatedOperatorAdd(VFXOperatorAdd add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(2, add.inputSlots.Count);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[1].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].expression);
            Assert.IsNotNull(add.outputSlots[0].expression as VFXExpressionAdd);
        }

        private void CheckIsolatedOperatorAbs(VFXOperatorAbs add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(1, add.inputSlots.Count);
            Assert.AreEqual(typeof(FloatN), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].expression);
            Assert.IsNotNull(add.outputSlots[0].expression as VFXExpressionAbs);
        }

        private void CheckConnectedAbs(VFXOperatorAbs abs)
        {
            Assert.IsTrue(abs.inputSlots[0].HasLink());
            Assert.AreEqual(1, abs.inputSlots[0].LinkedSlots.Count);
            Assert.IsTrue(abs.inputSlots[0].expression is VFXExpressionAdd);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VFXGraphAsset> write, Action<VFXGraphAsset> read)
        {
            var kTempAssetPathA = string.Format("{0}/Temp_{1}_A.asset", kTestAssetDir, suffixname);
            var kTempAssetPathB = string.Format("{0}/Temp_{1}_B.asset", kTestAssetDir, suffixname);
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);

            int hashCodeAsset = 0; //check reference are different between load & reload
            {
                var asset = ScriptableObject.CreateInstance<VFXGraphAsset>();
                hashCodeAsset = asset.GetHashCode();

                write(asset);

                AssetDatabase.CreateAsset(asset, kTempAssetPathA);
                asset.UpdateSubAssets();

                AssetDatabase.SaveAssets();
                AssetDatabase.CopyAsset(kTempAssetPathA, kTempAssetPathB);
                AssetDatabase.RemoveObject(asset);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.UnloadUnusedAssetsImmediate();
            {
                VFXGraphAsset asset = AssetDatabase.LoadAssetAtPath<VFXGraphAsset>(kTempAssetPathB);
                Assert.AreNotEqual(hashCodeAsset, asset.GetHashCode());

                read(asset);
            }
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);
        }

        private void WriteBasicOperators(VFXGraphAsset asset, bool spawnAbs, bool linkAbs)
        {
            var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
            asset.root.AddChild(add);

            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                var abs = ScriptableObject.CreateInstance<VFXOperatorAbs>();
                abs.position = new Vector2(64.0f, 64.0f);
                asset.root.AddChild(abs);
                CheckIsolatedOperatorAbs(abs);
                if (linkAbs)
                {
                    abs.inputSlots[0].Link(add.outputSlots[0]);
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void ReadBasicOperators(VFXGraphAsset asset, bool spawnAbs, bool linkAbs)
        {
            Assert.AreEqual(spawnAbs ? 2 : 1, asset.root.GetNbChildren());
            Assert.IsNotNull((VFXOperatorAdd)asset.root[0]);
            var add = (VFXOperatorAdd)asset.root[0];
            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                Assert.IsNotNull((VFXOperatorAbs)asset.root[1]);
                var abs = (VFXOperatorAbs)asset.root[1];
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
            InnerSaveAndReloadTest( suffix,
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
            string expectedValue = "xyx";
            Action<VFXGraphAsset> write = delegate (VFXGraphAsset asset)
            {
                var mask = ScriptableObject.CreateInstance<VFXOperatorComponentMask>();
                mask.settings = new VFXOperatorComponentMask.Settings()
                {
                    mask = expectedValue
                };
                asset.root.AddChild(mask);
                Assert.AreEqual(expectedValue, (mask.settings as VFXOperatorComponentMask.Settings).mask);
            };

            Action<VFXGraphAsset> read = delegate (VFXGraphAsset asset)
            {
                Assert.AreEqual(1, asset.root.GetNbChildren());
                Assert.IsInstanceOf(typeof(VFXOperatorComponentMask), asset.root[0]);
                var mask = asset.root[0] as VFXOperatorComponentMask;
                Assert.IsInstanceOf(typeof(VFXOperatorComponentMask.Settings), mask.settings);
                Assert.AreEqual(expectedValue, (mask.settings as VFXOperatorComponentMask.Settings).mask);
            };

            InnerSaveAndReloadTest("Mask", write, read);
        }

        [Test]
        public void SerializeParameter()
        {
            var name = "unity";
            Action<VFXGraphAsset> write = delegate (VFXGraphAsset asset)
            {
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                parameter.exposed = true;
                parameter.exposedName = name;
                asset.root.AddChild(parameter);
                Assert.AreEqual(VFXValueType.kFloat2, parameter.outputSlots[0].expression.ValueType);
            };

            Action<VFXGraphAsset> read = delegate (VFXGraphAsset asset)
            {
                var parameter = asset.root[0] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(true, parameter.exposed);
                Assert.AreEqual(parameter.exposedName, name);
                Assert.AreEqual(VFXValueType.kFloat2, parameter.outputSlots[0].expression.ValueType);
            };

            InnerSaveAndReloadTest("Parameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndParameter()
        {
            Action<VFXGraphAsset> write = delegate (VFXGraphAsset asset)
            {
                var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                asset.root.AddChild(add);
                asset.root.AddChild(parameter);
                add.inputSlots[0].Link(parameter.outputSlots[0]);

                Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].expression.ValueType);
            };

            Action<VFXGraphAsset> read = delegate (VFXGraphAsset asset)
            {
                var add = asset.root[0] as VFXOperatorAdd;
                var parameter = asset.root[1] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(VFXValueType.kFloat2, add.outputSlots[0].expression.ValueType);
            };

            InnerSaveAndReloadTest("ParameterAndOperator", write, read);
        }
    }
}