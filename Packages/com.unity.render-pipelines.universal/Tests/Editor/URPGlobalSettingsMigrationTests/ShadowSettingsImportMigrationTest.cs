using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class ShadowSettingsImportMigrationTest
    {
        const string k_Folder = "Assets/URP/MigrationTests/ShadowSettingsImport";

        static string AssetPath(string name) => $"{k_Folder}/{name}.asset";

        [SetUp]
        public void SetUp()
        {
            if (GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>() is not UniversalRenderPipelineGlobalSettings globalSettings
                || globalSettings.m_AssetVersion < UniversalRenderPipelineGlobalSettings.k_LastVersion)
            {
                Assert.Ignore("This test needs URP global settings at the last version");
                return;
            }

            CoreUtils.EnsureFolderTreeInAssetFilePath(AssetPath("Folder"));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_Folder);
        }

        static void WriteVersion15PipelineAsset(string name, float shadowDistance, int cascadeCount, float cascade2Split, Vector3 cascade4Split,
            float cascadeBorder, VolumeProfile profile)
        {
            string profileReference = profile == null
                ? "{fileID: 0}"
                : $"{{fileID: 11400000, guid: {AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile))}, type: 2}}";

            File.WriteAllText(AssetPath(name), FormattableString.Invariant($@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: bf2edee5c58d82540a51f03df9d42094, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  k_AssetVersion: 15
  k_AssetPreviousVersion: 15
  m_ShadowDistance: {shadowDistance}
  m_ShadowCascadeCount: {cascadeCount}
  m_Cascade2Split: {cascade2Split}
  m_Cascade3Split: {{x: 0.1, y: 0.3}}
  m_Cascade4Split: {{x: {cascade4Split.x}, y: {cascade4Split.y}, z: {cascade4Split.z}}}
  m_CascadeBorder: {cascadeBorder}
  m_VolumeProfile: {profileReference}
"));
        }

        static string Link(string name) =>
            Regex.Escape("<link=\"srpasset='") + "[0-9a-f]{32}" + Regex.Escape("'\"><color=") + "#[0-9A-Fa-f]{6}" +
            Regex.Escape($"><u>{name}</u></color></link>");

        static string CopyLine(string asset, string source) =>
            $"'{Link(asset)}' now uses '{Link($"{asset}_VolumeProfile")}', a copy of '{Link(source)}'\\.";

        static string RemainingLine(string source, string user) => $"'{Link(source)}' is still used by '{Link(user)}'\\.";

        static void AssertMigrated(UniversalRenderPipelineAsset urpAsset, float shadowDistance, int cascadeCount, Vector3 splits, float cascadeBorder)
        {
            Assert.IsNotNull(urpAsset, "The pipeline asset was not imported");
            Assert.IsFalse(urpAsset.m_ShadowSettingsMigrationPending, $"'{urpAsset.name}' must be marked as migrated after its import");
            string savedAsset = File.ReadAllText(AssetDatabase.GetAssetPath(urpAsset));
            StringAssert.Contains("k_AssetVersion: 16", savedAsset, $"'{urpAsset.name}' must be saved at the last version");
            StringAssert.Contains("m_ShadowSettingsMigrationPending: 0", savedAsset, $"'{urpAsset.name}' must be saved as migrated");

            Assert.IsNotNull(urpAsset.volumeProfile, $"'{urpAsset.name}' has no volume profile");
            Assert.IsTrue(urpAsset.volumeProfile.TryGet(out ShadowsVolumeOverride shadows), $"The volume profile of '{urpAsset.name}' has no Shadows override");
            Assert.IsTrue(EditorUtility.IsPersistent(shadows) && AssetDatabase.GetAssetPath(shadows) == AssetDatabase.GetAssetPath(urpAsset.volumeProfile),
                "The Shadows override must be saved inside the volume profile asset");

            foreach (var parameter in shadows.parameters)
                Assert.IsTrue(parameter.overrideState, "The migrated Shadows override must override every parameter");

            Assert.AreEqual(shadowDistance, shadows.maxShadowDistance.value, 1e-5f, "maxShadowDistance");
            Assert.AreEqual(cascadeCount, shadows.cascadeCount.value, "cascadeCount");
            Assert.AreEqual(splits.x, shadows.cascadeSplit0.value, 1e-5f, "cascadeSplit0");
            Assert.AreEqual(splits.y, shadows.cascadeSplit1.value, 1e-5f, "cascadeSplit1");
            Assert.AreEqual(splits.z, shadows.cascadeSplit2.value, 1e-5f, "cascadeSplit2");
            Assert.AreEqual(cascadeBorder, shadows.lastCascadeBorder.value, 1e-5f, "lastCascadeBorder");
        }

        [Test]
        [Description("A pipeline asset from before the Shadows volume override that is imported after the global settings migration gets its shadow settings in a volume profile.")]
        public void ImportedVersion15AssetIsMigrated()
        {
            var splits = new Vector3(0.067f, 0.2f, 0.467f);
            WriteVersion15PipelineAsset("Imported", 20f, 4, 0.25f, splits, 0.2f, null);
            AssetDatabase.ImportAsset(AssetPath("Imported"), ImportAssetOptions.ForceSynchronousImport);

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("Imported"));
            AssertMigrated(urpAsset, 20f, 4, splits, 0.2f);
            Assert.AreEqual(AssetPath("Imported_VolumeProfile"), AssetDatabase.GetAssetPath(urpAsset.volumeProfile));
        }

        [Test]
        [Description("Two pipeline assets from before the Shadows volume override that share a volume profile and are imported together each keep their own shadow settings.")]
        public void ImportedVersion15AssetsSharingAProfileGetTheirOwnValues()
        {
            var shared = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(shared, AssetPath("Shared"));

            var splits = new Vector3(0.1f, 0.3f, 0.6f);
            WriteVersion15PipelineAsset("SharedA", 30f, 4, 0.25f, splits, 0.1f, shared);
            WriteVersion15PipelineAsset("SharedB", 80f, 2, 0.4f, splits, 0.15f, shared);
            LogAssert.Expect(LogType.Warning, new Regex(CopyLine("SharedA", "Shared") + "\n" + CopyLine("SharedB", "Shared")));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var first = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("SharedA"));
            var second = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("SharedB"));

            Assert.AreEqual(AssetPath("SharedA_VolumeProfile"), AssetDatabase.GetAssetPath(first.volumeProfile), "The first asset must use its own copy");
            Assert.AreEqual(AssetPath("SharedB_VolumeProfile"), AssetDatabase.GetAssetPath(second.volumeProfile), "The second asset must use its own copy");
            Assert.IsFalse(shared.Has<ShadowsVolumeOverride>(), "The shared profile must not get a Shadows override");
            AssertMigrated(first, 30f, 4, splits, 0.1f);
            AssertMigrated(second, 80f, 2, new Vector3(0.4f, splits.y, splits.z), 0.15f);
        }

        [Test]
        [Description("Pipeline assets from before the Shadows volume override that share a volume profile and have the same shadow settings share one copy when they are imported together.")]
        public void ImportedVersion15AssetsWithTheSameValuesShareOneCopy()
        {
            var shared = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(shared, AssetPath("Shared"));

            var splits = new Vector3(0.1f, 0.3f, 0.6f);
            WriteVersion15PipelineAsset("SameA", 30f, 4, 0.25f, splits, 0.1f, shared);
            WriteVersion15PipelineAsset("SameB", 30f, 4, 0.25f, splits, 0.1f, shared);
            LogAssert.Expect(LogType.Warning, new Regex($"'{Link("SameA")}', '{Link("SameB")}' now use '{Link("Shared_Shadows")}', a copy of '{Link("Shared")}'\\."));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var first = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("SameA"));
            var second = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("SameB"));

            Assert.AreEqual(AssetPath("Shared_Shadows"), AssetDatabase.GetAssetPath(first.volumeProfile), "The first asset must use the shared copy");
            Assert.AreSame(first.volumeProfile, second.volumeProfile, "Both assets must use the same copy");
            Assert.AreEqual(2, AssetDatabase.FindAssets("t:VolumeProfile", new[] { k_Folder }).Length, "The import must create exactly one copy");
            AssertMigrated(first, 30f, 4, splits, 0.1f);
            AssertMigrated(second, 30f, 4, splits, 0.1f);
        }

        [Test]
        [Description("A pipeline asset from before the Shadows volume override that shares a volume profile with an already migrated pipeline asset gets its own copy, and the warning names the migrated asset.")]
        public void ImportedVersion15AssetSharingAProfileWithAMigratedAssetGetsACopy()
        {
            var shared = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(shared, AssetPath("Shared"));

            var migrated = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            migrated.volumeProfile = shared;
            AssetDatabase.CreateAsset(migrated, AssetPath("Migrated"));

            var splits = new Vector3(0.1f, 0.3f, 0.6f);
            WriteVersion15PipelineAsset("Imported", 30f, 4, 0.25f, splits, 0.1f, shared);
            LogAssert.Expect(LogType.Warning, new Regex(CopyLine("Imported", "Shared") + "\n" + RemainingLine("Shared", "Migrated")));
            AssetDatabase.ImportAsset(AssetPath("Imported"), ImportAssetOptions.ForceSynchronousImport);

            var imported = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("Imported"));
            Assert.AreEqual(AssetPath("Imported_VolumeProfile"), AssetDatabase.GetAssetPath(imported.volumeProfile), "The imported asset must use its own copy");
            Assert.AreSame(shared, migrated.volumeProfile, "The migrated asset must keep its volume profile");
            Assert.IsFalse(shared.Has<ShadowsVolumeOverride>(), "The shared profile must not get a Shadows override");
            AssertMigrated(imported, 30f, 4, splits, 0.1f);
        }

        [Test]
        [Description("A pipeline asset from before the Shadows volume override that the migration cannot process is still at the last version, so that builds accept it.")]
        public void UnprocessedVersion15AssetIsAtLastVersion()
        {
            var globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>() as UniversalRenderPipelineGlobalSettings;
            int previousVersion = globalSettings.m_AssetVersion;
            globalSettings.m_AssetVersion = 16;
            try
            {
                WriteVersion15PipelineAsset("Unprocessed", 20f, 4, 0.25f, new Vector3(0.067f, 0.2f, 0.467f), 0.2f, null);
                AssetDatabase.ImportAsset(AssetPath("Unprocessed"), ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                globalSettings.m_AssetVersion = previousVersion;
            }

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath("Unprocessed"));
            Assert.IsTrue(urpAsset.IsAtLastVersion(), "An asset loaded from version 15 must be at the last version");
            Assert.IsTrue(urpAsset.m_ShadowSettingsMigrationPending, "An asset that was not migrated must stay pending");
            Assert.IsNull(urpAsset.volumeProfile, "An asset that was not migrated must not get a volume profile");
        }

        [Test]
        [Description("A new pipeline asset is at the last version and has no shadow settings to migrate.")]
        public void NewAssetHasNoPendingMigration()
        {
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                Assert.IsTrue(urpAsset.IsAtLastVersion(), "A new asset must be at the last version");
                Assert.IsFalse(urpAsset.m_ShadowSettingsMigrationPending, "A new asset must not wait for a shadow settings migration");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(urpAsset);
            }
        }
    }
}
