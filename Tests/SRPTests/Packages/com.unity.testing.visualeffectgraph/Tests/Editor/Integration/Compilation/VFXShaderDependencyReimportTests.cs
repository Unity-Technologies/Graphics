using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXShaderDependencyReimportTests
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

        [Test]
        public void ArtifactHashUnchanged_OnReimporting_ShaderGraph_With_VfxTarget()
        {
            var path = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/SG_Lit_VfxTarget.shadergraph";

            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            var initialHash = AssetDatabase.GetAssetDependencyHash(path);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            var finalHash = AssetDatabase.GetAssetDependencyHash(path);

            Assert.AreEqual(initialHash, finalHash);
        }

        class ImportedAssetsRecorder : AssetPostprocessor
        {
            public static bool Enabled;
            public static readonly List<string> ImportedAssets = new List<string>();

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (Enabled)
                    ImportedAssets.AddRange(importedAssets);
            }
        }

        static void CreateSystemFromCustomTemplate(VFXGraph graph, string templatePathWithoutExtension)
        {
            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            graph.AddChild(output);

            var contextInitialize = ScriptableObject.CreateInstance<VFXTestInitializeFromCustomTemplate>();
            contextInitialize.templatePathWithoutExtension = templatePathWithoutExtension;
            var contextUpdate = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(contextUpdate);
            contextInitialize.LinkTo(contextUpdate);

            contextUpdate.LinkTo(output);
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);
        }

        [Test, Description("A code generation template is an input of every visual effect that generates shaders from it, so editing one must reimport those effects, and only those.")]
        public void Editing_CodeGeneration_Template_Reimports_Dependent_VisualEffect()
        {
            const string templateExtension = ".template";
            var templatePath = $"{VFXTestCommon.tempBasePath}VFXTestInit_{Guid.NewGuid()}{templateExtension}";

            Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            File.Copy("Packages/com.unity.visualeffectgraph/Shaders/VFXInit.template", templatePath);
            AssetDatabase.ImportAsset(templatePath);
            Assert.IsFalse(AssetDatabase.GUIDFromAssetPath(templatePath).Empty(), $"'{templatePath}' must be imported for the code generator to register it as a dependency.");

            var templatePathWithoutExtension = templatePath.Substring(0, templatePath.Length - templateExtension.Length);

            var dependentPaths = new List<string>();
            for (var i = 0; i < 3; ++i)
            {
                var dependentGraph = VFXTestCommon.MakeTemporaryGraph();
                CreateSystemFromCustomTemplate(dependentGraph, templatePathWithoutExtension);
                VFXTestCommon.ReimportVFXGraph(dependentGraph);

                var dependentPath = AssetDatabase.GetAssetPath(dependentGraph);
                Assert.IsNotEmpty(dependentPath);
                AssetDatabase.ImportAsset(dependentPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                dependentPaths.Add(dependentPath);
            }

            var unrelatedPath = AssetDatabase.GetAssetPath(VFXTestCommon.CreateGraph_And_System());
            Assert.IsNotEmpty(unrelatedPath);

            ImportedAssetsRecorder.ImportedAssets.Clear();
            ImportedAssetsRecorder.Enabled = true;
            try
            {
                File.AppendAllText(templatePath, "\n");
                AssetDatabase.Refresh();
            }
            finally
            {
                ImportedAssetsRecorder.Enabled = false;
            }

            foreach (var dependentPath in dependentPaths)
                Assert.That(ImportedAssetsRecorder.ImportedAssets, Does.Contain(dependentPath));

            Assert.That(ImportedAssetsRecorder.ImportedAssets, Does.Not.Contain(unrelatedPath));
        }
    }
}
