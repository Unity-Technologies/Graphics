using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXComputeShaderAssetIdentityTest
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static string CreateTwoSystemGraph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.ReimportVFXGraph(graph);
            return AssetDatabase.GetAssetPath(graph);
        }

        static ComputeShader[] LoadComputeShaders(string vfxPath)
        {
            ComputeShader[] computeShaders = AssetDatabase.LoadAllAssetsAtPath(vfxPath).OfType<ComputeShader>().ToArray();
            Assert.GreaterOrEqual(computeShaders.Length, 4);
            return computeShaders;
        }

        static long GetLocalFileId(ComputeShader computeShader, out string guid)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(computeShader, out guid, out long localId));
            return localId;
        }

        static SortedSet<long> CollectLocalFileIds(string vfxPath)
        {
            var ids = new SortedSet<long>();
            foreach (ComputeShader computeShader in LoadComputeShaders(vfxPath))
                Assert.IsTrue(ids.Add(GetLocalFileId(computeShader, out _)), "Duplicate local file ID");
            return ids;
        }

        static void AssertSerializedAssetGuid(SerializedObject serializedObject, string assetGuid)
        {
            SerializedProperty guidProperty = serializedObject.FindProperty("m_AssetGUID");
            Assert.IsNotNull(guidProperty);

            SerializedProperty end = guidProperty.GetEndProperty();
            SerializedProperty word = guidProperty.Copy();
            bool enterChildren = true;
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(word.Next(enterChildren) && !SerializedProperty.EqualContents(word, end));
                enterChildren = false;

                uint expected = 0;
                for (int j = 0; j < 8; j++)
                    expected |= System.Convert.ToUInt32(assetGuid[i * 8 + j].ToString(), 16) << (j * 4);
                Assert.AreEqual(expected, unchecked((uint)word.longValue), "m_AssetGUID word " + i);
            }
        }

        static string SaveSingleVariantJson(ComputeShader computeShader)
        {
            var collection = new GraphicsStateCollection();
            Assert.IsTrue(collection.AddComputeVariant(computeShader, 0, new LocalKeyword[0]));
            string filePath = Path.Combine(Application.temporaryCachePath, "VFXComputeShaderAssetIdentity.graphicsstate");
            Assert.IsTrue(collection.SaveToFile(filePath));
            string json = File.ReadAllText(filePath);
            File.Delete(filePath);
            return json;
        }

        static void AssertStampedIdentity(string vfxPath)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(vfxPath);
            foreach (ComputeShader computeShader in LoadComputeShaders(vfxPath))
            {
                long localId = GetLocalFileId(computeShader, out string guid);
                Assert.AreEqual(assetGuid, guid);
                Assert.AreNotEqual(0L, localId);

                var serializedShader = new SerializedObject(computeShader);
                AssertSerializedAssetGuid(serializedShader, assetGuid);

                SerializedProperty stamped = serializedShader.FindProperty("m_AssetLocalIdentifierInFile");
                Assert.IsNotNull(stamped);
                Assert.AreEqual(localId, stamped.longValue);

                string json = SaveSingleVariantJson(computeShader);
                StringAssert.IsMatch("\"computeShaderAssetGUID\"\\s*:\\s*\"" + guid + "\"", json);
                StringAssert.IsMatch("\"computeShaderAssetLocalIdentifierInFile\"\\s*:\\s*" + localId + "\\b", json);
            }
        }

        [Test]
        public void VFXImport_StampsAssetGuidAndDistinctLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();

            CollectLocalFileIds(vfxPath);
            AssertStampedIdentity(vfxPath);
        }

        [Test]
        public void VFXRename_KeepsComputeLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();
            SortedSet<long> before = CollectLocalFileIds(vfxPath);

            Assert.AreEqual("", AssetDatabase.RenameAsset(vfxPath, "VFXComputeIdentityRenamed"));
            string renamedPath = VFXTestCommon.tempBasePath + "VFXComputeIdentityRenamed.vfx";
            AssetDatabase.ImportAsset(renamedPath, ImportAssetOptions.ForceUpdate);

            CollectionAssert.AreEqual(before, CollectLocalFileIds(renamedPath));
            AssertStampedIdentity(renamedPath);
        }

        [Test]
        public void VFXMove_KeepsComputeLocalFileIds()
        {
            string vfxPath = CreateTwoSystemGraph();
            SortedSet<long> before = CollectLocalFileIds(vfxPath);

            AssetDatabase.CreateFolder(VFXTestCommon.tempBasePath.TrimEnd('/'), "Moved");
            Assert.IsTrue(AssetDatabase.IsValidFolder(VFXTestCommon.tempBasePath + "Moved"));
            string movedPath = VFXTestCommon.tempBasePath + "Moved/" + Path.GetFileName(vfxPath);
            Assert.AreEqual("", AssetDatabase.MoveAsset(vfxPath, movedPath));
            AssetDatabase.ImportAsset(movedPath, ImportAssetOptions.ForceUpdate);

            CollectionAssert.AreEqual(before, CollectLocalFileIds(movedPath));
            AssertStampedIdentity(movedPath);
        }

        [Test]
        public void VFXComputeVariants_RoundTripGraphicsStateCollection_ResolveToOwnSubAsset()
        {
            string vfxPath = CreateTwoSystemGraph();
            ComputeShader[] computeShaders = LoadComputeShaders(vfxPath);

            string collectionPath = VFXTestCommon.tempBasePath + "VFXComputeIdentity.graphicsstate";
            var collection = new GraphicsStateCollection();
            foreach (ComputeShader computeShader in computeShaders)
                Assert.IsTrue(collection.AddComputeVariant(computeShader, 0, new LocalKeyword[0]));
            Assert.AreEqual(computeShaders.Length, collection.computeVariantCount);
            Assert.IsTrue(collection.SaveToFile(Path.GetFullPath(collectionPath)));

            AssetDatabase.ImportAsset(collectionPath, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(collectionPath);
            Assert.AreEqual(computeShaders.Length, loaded.computeVariantCount);

            var variants = new List<GraphicsStateCollection.ComputeShaderVariant>();
            loaded.GetComputeVariants(variants);
            var resolved = new HashSet<Object>();
            foreach (GraphicsStateCollection.ComputeShaderVariant variant in variants)
            {
                Assert.IsNotNull(variant.shader);
                Assert.IsTrue(resolved.Add(variant.shader), "Two rows resolved to the same ComputeShader");
            }
            CollectionAssert.AreEquivalent(computeShaders, resolved);
            foreach (ComputeShader computeShader in computeShaders)
                Assert.IsTrue(loaded.ContainsComputeVariant(computeShader, 0, new LocalKeyword[0]));
        }

        [Test]
        public void AddComputeVariants_AddsKernelZeroOfEachCompute()
        {
            string vfxPath = CreateTwoSystemGraph();
            ComputeShader[] computeShaders = LoadComputeShaders(vfxPath);
            var visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);

            var collection = new GraphicsStateCollection();
            Assert.IsTrue(collection.AddComputeVariants(new[] { visualEffectAsset }, new GlobalKeyword[0]));
            Assert.AreEqual(computeShaders.Length, collection.computeVariantCount);
            foreach (ComputeShader computeShader in computeShaders)
                Assert.IsTrue(collection.ContainsComputeVariant(computeShader, 0, computeShader.enabledKeywords));

            Assert.IsFalse(collection.AddComputeVariants(new[] { visualEffectAsset }, new GlobalKeyword[0]));

            var manualCollection = new GraphicsStateCollection();
            foreach (ComputeShader computeShader in computeShaders)
                Assert.IsTrue(manualCollection.AddComputeVariant(computeShader, 0, computeShader.enabledKeywords));
            CollectionAssert.AreEquivalent(GetComputeVariantKeys(manualCollection), GetComputeVariantKeys(collection));
        }

        static List<object> GetComputeVariantKeys(GraphicsStateCollection collection)
        {
            var variants = new List<GraphicsStateCollection.ComputeShaderVariant>();
            collection.GetComputeVariants(variants);
            return variants.Select(v => (object)new
            {
                v.shader,
                v.kernelIndex,
                keywords = string.Join(" ", v.keywords.Select(k => k.name).OrderBy(n => n))
            }).ToList();
        }
    }
}
