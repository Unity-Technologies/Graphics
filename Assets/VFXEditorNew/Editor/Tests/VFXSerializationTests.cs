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
                VFXModelContainer asset = ScriptableObject.CreateInstance<VFXModelContainer>();
                InitAsset(asset);
                AssetDatabase.CreateAsset(asset,kTestAssetPath);
            }
        }

        [Test]
        public void SerializeModel()
        {
            VFXModelContainer assetSrc = ScriptableObject.CreateInstance<VFXModelContainer>();
            VFXModelContainer assetDst = ScriptableObject.CreateInstance<VFXModelContainer>();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }

        [Test]
        public void LoadAssetFromPath()
        {
            VFXModelContainer asset = AssetDatabase.LoadAssetAtPath<VFXModelContainer>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void InitAsset(VFXModelContainer asset)
        {
            asset.m_Roots.Clear();

            VFXSystem system0 = new VFXSystem();
            system0.AddChild(new VFXContext(VFXContextDesc.CreateBasic(VFXContextType.kInit)));
            system0.AddChild(new VFXContext(VFXContextDesc.CreateBasic(VFXContextType.kUpdate)));
            system0.AddChild(new VFXContext(VFXContextDesc.CreateBasic(VFXContextType.kOutput)));

            VFXSystem system1 = new VFXSystem();
            system1.AddChild(new VFXContext(VFXContextDesc.CreateBasic(VFXContextType.kInit)));
            system1.AddChild(new VFXContext(VFXContextDesc.CreateBasic(VFXContextType.kOutput)));

            // Add some block
            var block0 = new VFXBlock(new VFXInitBlockTest());
            var block1 = new VFXBlock(new VFXUpdateBlockTest());
            var block2 = new VFXBlock(new VFXOutputBlockTest());

            system0.GetChild(0).AddChild(block0);
            system0.GetChild(1).AddChild(block1);
            system0.GetChild(2).AddChild(block2);

            asset.m_Roots.Add(system0);
            asset.m_Roots.Add(system1);
        }

        private void CheckAsset(VFXModelContainer asset)
        {
            Assert.AreEqual(2, asset.m_Roots.Count);
            Assert.AreEqual(3, asset.m_Roots[0].GetNbChildren());
            Assert.AreEqual(2, asset.m_Roots[1].GetNbChildren());

            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(0).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(1).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(2).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[1])).GetChild(0).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[1])).GetChild(1).Desc);

            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(0).GetChild(0).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(1).GetChild(0).Desc);
            Assert.IsNotNull(((VFXSystem)(asset.m_Roots[0])).GetChild(2).GetChild(0).Desc);
        }
    }
}