using System;
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

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
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

        // TODOPAUL fix this test for operator
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
            VFXOperator add = new VFXOperatorAdd();

            system0[0].AddChild(block0);
            system0[1].AddChild(block1);
            system0[2].AddChild(block2);

            asset.root.AddChild(system0);
            asset.root.AddChild(system1);
            //asset.root.AddChild(add);
        }

        private void CheckAsset(VFXGraphAsset asset)
        {
            Assert.AreEqual(2, asset.root.GetNbChildren());

            Assert.AreEqual(3, asset.root[0].GetNbChildren());
            Assert.AreEqual(2, asset.root[1].GetNbChildren());
            //Assert.AreEqual(0, asset.root[2].GetNbChildren());

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
            //Assert.IsNotNull((VFXOperatorAdd)asset.root[2]);
        }
    }
}