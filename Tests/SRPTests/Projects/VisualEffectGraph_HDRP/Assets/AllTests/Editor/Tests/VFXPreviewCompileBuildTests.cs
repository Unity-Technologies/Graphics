using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXPreviewCompileBuildTests
    {
        const string k_BundleFolder = "Temp/VFXPreviewCompileBuildTests";
        const string k_BundleName = "vfxpreviewcompile";

        AssetBundle m_Bundle;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget))
                Assert.Ignore($"Requires player support for {buildTarget} to build AssetBundles.");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Bundle != null)
                m_Bundle.Unload(true);
            m_Bundle = null;
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
            if (Directory.Exists(k_BundleFolder))
                Directory.Delete(k_BundleFolder, true);
        }

        // Only m_Systems is in builds, m_EditorSystems also references shaders but is editor only
        static List<Object> GetRuntimeProcessors(VisualEffectAsset asset)
        {
            var processors = new List<Object>();
            var property = new SerializedObject(asset).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyPath.StartsWith("m_Systems") && property.name == "processor" && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                    processors.Add(property.objectReferenceValue);
            }
            return processors;
        }

        // An open graph window does this, replacing the loaded asset's shaders with ones that aren't saved
        static void PreviewCompile(VFXGraph graph, VisualEffectAsset asset)
        {
            graph.CompileAndUpdateAsset(asset);
            var processors = GetRuntimeProcessors(asset);
            Assert.IsNotEmpty(processors);
            Assert.That(processors, Has.None.Matches<Object>(EditorUtility.IsPersistent), "The preview compile should reference shaders that aren't saved.");
        }

        VisualEffectAsset BuildAndLoad(string path)
        {
            Directory.CreateDirectory(k_BundleFolder);
            var build = new AssetBundleBuild { assetBundleName = k_BundleName, assetNames = new[] { path } };
            var manifest = BuildPipeline.BuildAssetBundles(k_BundleFolder, new[] { build }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            Assert.IsNotNull(manifest, "The asset bundle build failed.");

            m_Bundle = AssetBundle.LoadFromFile(Path.Combine(k_BundleFolder, k_BundleName));
            Assert.IsNotNull(m_Bundle);
            var builtAsset = m_Bundle.LoadAsset<VisualEffectAsset>(path);
            Assert.IsNotNull(builtAsset);
            return builtAsset;
        }

        [Test]
        public void Build_AfterPreviewCompile_KeepsImportedShaders()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var path = AssetDatabase.GetAssetPath(graph);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            var importedCount = GetRuntimeProcessors(asset).Count;
            Assert.Greater(importedCount, 0, "The imported asset should reference its shaders.");

            PreviewCompile(graph, asset);

            Assert.AreEqual(importedCount, GetRuntimeProcessors(BuildAndLoad(path)).Count);
        }

        [Test]
        public void Build_AfterEditAndSave_UsesSavedShaders()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var path = AssetDatabase.GetAssetPath(graph);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            var importedCount = GetRuntimeProcessors(asset).Count;
            PreviewCompile(graph, asset);

            // Edit and save while the window keeps preview compiling, the build must use the saved graph
            VFXTestCommon.CreateSystem(graph);
            PreviewCompile(graph, asset);
            VFXTestCommon.ReimportVFXGraph(graph);
            asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            var savedCount = GetRuntimeProcessors(asset).Count;
            Assert.Greater(savedCount, importedCount, "The saved asset should reference the added system's shaders.");
            PreviewCompile(graph, asset);

            Assert.AreEqual(savedCount, GetRuntimeProcessors(BuildAndLoad(path)).Count);
        }
    }
}
