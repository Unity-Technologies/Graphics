using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXAssetCreationTest
    {
        [SetUp]
        public void Init()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static string TempPath(string extension)
        {
            Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            return $"{VFXTestCommon.tempBasePath}{Guid.NewGuid()}.{extension}";
        }

        // Loads independently of whatever is already in memory, so it also catches a
        // resource or child model left over from an overwrite.
        static void AssertFileHoldsOnlyResourceAndGraph(string path)
        {
            var loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            try
            {
                var resources = loaded.OfType<VisualEffectResource>().ToArray();
                var graphs = loaded.OfType<VFXGraph>().ToArray();

                Assert.AreEqual(2, loaded.Length);
                Assert.AreEqual(1, resources.Length);
                Assert.AreEqual(1, graphs.Length);
                Assert.AreEqual(graphs[0], resources[0].graph);
            }
            finally
            {
                foreach (var obj in loaded)
                    Object.DestroyImmediate(obj);
            }
        }

        static int CountObjectsInFile(string path)
        {
            var loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            foreach (var obj in loaded)
                Object.DestroyImmediate(obj);
            return loaded.Length;
        }

        [Test]
        public void CreateNewAsset_ProducesVisualEffectAssetWithGraph()
        {
            var path = TempPath("vfx");
            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(path);

            Assert.IsNotNull(asset);
            Assert.AreEqual(typeof(VisualEffectAsset), AssetDatabase.GetMainAssetTypeAtPath(path));
            Assert.IsNotNull(asset.GetResource());
            Assert.IsNotNull(asset.GetResource().GetGraph());
        }

        [Test]
        public void CreateNew_SubgraphOperator_ProducesSubgraphWithGraph()
        {
            var path = TempPath("vfxoperator");
            var asset = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>(path);

            Assert.IsNotNull(asset);
            Assert.AreEqual(typeof(VisualEffectSubgraphOperator), AssetDatabase.GetMainAssetTypeAtPath(path));
            Assert.IsNotNull(asset.GetResource());
            Assert.IsNotNull(asset.GetResource().GetGraph());
        }

        [Test]
        public void CreateNew_SubgraphBlock_ProducesSubgraphWithGraph()
        {
            var path = TempPath("vfxblock");
            var asset = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphBlock>(path);

            Assert.IsNotNull(asset);
            Assert.AreEqual(typeof(VisualEffectSubgraphBlock), AssetDatabase.GetMainAssetTypeAtPath(path));
            Assert.IsNotNull(asset.GetResource());
            Assert.IsNotNull(asset.GetResource().GetGraph());
        }

        [Test]
        public void CreateNew_FileOnDiskHoldsOneResourceReferencingItsGraph()
        {
            var path = TempPath("vfx");
            VisualEffectAssetEditorUtility.CreateNewAsset(path);

            AssertFileHoldsOnlyResourceAndGraph(path);
        }

        [Test]
        public void CreateNew_OverExistingAsset_KeepsGuidAndHoldsOneResource()
        {
            var path = TempPath("vfx");
            var firstAsset = VisualEffectAssetEditorUtility.CreateNewAsset(path);
            var guid = AssetDatabase.AssetPathToGUID(path);

            var graph = firstAsset.GetResource().GetGraph();
            graph.AddChild(ScriptableObject.CreateInstance<VFXBasicSpawner>());
            firstAsset.GetResource().WriteAssetWithSubAssets();

            Assert.Greater(CountObjectsInFile(path), 2);

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(path);

            Assert.IsNotNull(asset);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            AssertFileHoldsOnlyResourceAndGraph(path);
        }

        [Test]
        public void CreateNew_DoesNotLogAGraphChange()
        {
            var messages = new List<string>();
            Application.LogCallback collect = (message, stackTrace, type) => messages.Add(message);
            Application.logMessageReceived += collect;
            try
            {
                VisualEffectAssetEditorUtility.CreateNewAsset(TempPath("vfx"));
                VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>(TempPath("vfxoperator"));
                VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphBlock>(TempPath("vfxblock"));
            }
            finally
            {
                Application.logMessageReceived -= collect;
            }

            Assert.IsFalse(
                messages.Any(m => m.Contains("Changing the graph of a VisualEffectResource")),
                string.Join("\n", messages));
        }

        [Test]
        public void CreateNew_InMissingDirectory_Throws()
        {
            var missingDirectory = $"{VFXTestCommon.tempBasePath}missing_{Guid.NewGuid()}/";
            var path = $"{missingDirectory}asset.vfx";

            Assert.IsFalse(Directory.Exists(missingDirectory));
            Assert.Throws<DirectoryNotFoundException>(() => VisualEffectAssetEditorUtility.CreateNewAsset(path));
            Assert.IsFalse(Directory.Exists(missingDirectory));
        }
    }
}
