using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class ShadowSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<URPDefaultVolumeProfileSettings>
    {
        const string k_Folder = "Assets/URP/MigrationTests/ShadowSettings";

        static readonly Vector3 k_Cascade4Split = new Vector3(0.1f, 0.3f, 0.6f);

        static string AssetPath(string name) => $"{k_Folder}/{name}.asset";
        static string MigratedProfilePath(string name) => $"{k_Folder}/{name}_VolumeProfile.asset";

        static UniversalRenderPipelineAsset CreatePipelineAsset(string name, float shadowDistance, int cascadeCount, float cascadeBorder,
            float cascade2Split = 0.25f, Vector2 cascade3Split = default, VolumeProfile profile = null)
        {
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urpAsset.m_ShadowSettingsMigrationPending = true;
#pragma warning disable 618 // Type or member is obsolete
            urpAsset.m_ObsoleteShadowDistance = shadowDistance;
            urpAsset.m_ObsoleteShadowCascadeCount = cascadeCount;
            urpAsset.m_ObsoleteCascade2Split = cascade2Split;
            urpAsset.m_ObsoleteCascade3Split = cascade3Split == default ? new Vector2(0.1f, 0.3f) : cascade3Split;
            urpAsset.m_ObsoleteCascade4Split = k_Cascade4Split;
            urpAsset.m_ObsoleteCascadeBorder = cascadeBorder;
#pragma warning restore 618
            urpAsset.volumeProfile = profile;

            string path = AssetPath(name);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            AssetDatabase.CreateAsset(urpAsset, path);
            return urpAsset;
        }

        static UniversalRenderPipelineAsset CreateDefaultPipelineAsset(string name, VolumeProfile profile = null)
        {
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urpAsset.m_ShadowSettingsMigrationPending = true;
            urpAsset.volumeProfile = profile;

            string path = AssetPath(name);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            AssetDatabase.CreateAsset(urpAsset, path);
            return urpAsset;
        }

        static VolumeProfile CreateProfileAsset(string name)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string path = AssetPath(name);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        static T AddPersistedOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var volumeOverride = profile.Add<T>(overrides: true);
            AssetDatabase.AddObjectToAsset(volumeOverride, profile);
            return volumeOverride;
        }

        static bool AllParametersOverridden(VolumeComponent component)
        {
            foreach (var parameter in component.parameters)
            {
                if (!parameter.overrideState)
                    return false;
            }
            return true;
        }

        // AssetDatabase.IsSubAsset is false for volume components, which are hidden in the hierarchy
        static bool IsSavedInProfile(VolumeComponent component, VolumeProfile profile)
        {
            return EditorUtility.IsPersistent(component) && AssetDatabase.GetAssetPath(component) == AssetDatabase.GetAssetPath(profile);
        }

        static bool HasMigratedValues(VolumeProfile profile, float shadowDistance, int cascadeCount, Vector3 splits, float cascadeBorder, out string message)
        {
            if (profile == null || !profile.TryGet(out ShadowsVolumeOverride shadows))
            {
                message = "The pipeline asset has no volume profile with a Shadows override";
                return false;
            }

            if (!AllParametersOverridden(shadows))
            {
                message = "The migrated Shadows override must override every parameter";
                return false;
            }

            if (!Mathf.Approximately(shadows.maxShadowDistance.value, shadowDistance) ||
                shadows.cascadeCount.value != cascadeCount ||
                !Mathf.Approximately(shadows.cascadeSplit0.value, splits.x) ||
                !Mathf.Approximately(shadows.cascadeSplit1.value, splits.y) ||
                !Mathf.Approximately(shadows.cascadeSplit2.value, splits.z) ||
                !Mathf.Approximately(shadows.lastCascadeBorder.value, cascadeBorder))
            {
                message = $"Values were not copied: distance {shadows.maxShadowDistance.value}, count {shadows.cascadeCount.value}, " +
                    $"splits ({shadows.cascadeSplit0.value}, {shadows.cascadeSplit1.value}, {shadows.cascadeSplit2.value}), border {shadows.lastCascadeBorder.value}";
                return false;
            }

            if (!IsSavedInProfile(shadows, profile))
            {
                message = "The Shadows override must be saved inside the volume profile asset";
                return false;
            }

            message = string.Empty;
            return true;
        }

        static bool UsesCopyWithBloom(UniversalRenderPipelineAsset urpAsset, out string message) =>
            UsesCopyWithBloom(urpAsset, MigratedProfilePath(urpAsset.name), out message);

        static bool UsesCopyWithBloom(UniversalRenderPipelineAsset urpAsset, string copyPath, out string message)
        {
            if (AssetDatabase.GetAssetPath(urpAsset.volumeProfile) != copyPath)
            {
                message = $"'{urpAsset.name}' must use a copy at {copyPath}, got '{AssetDatabase.GetAssetPath(urpAsset.volumeProfile)}'";
                return false;
            }

            if (!urpAsset.volumeProfile.TryGet(out Bloom copiedBloom) || !IsSavedInProfile(copiedBloom, urpAsset.volumeProfile))
            {
                message = $"The copy of '{urpAsset.name}' must keep the other overrides of the original volume profile";
                return false;
            }

            message = string.Empty;
            return true;
        }

        static string Link(string path) =>
            Regex.Escape("<link=\"srpasset='") + "[0-9a-f]{32}" + Regex.Escape("'\"><color=") + "#[0-9A-Fa-f]{6}" +
            Regex.Escape($"><u>{System.IO.Path.GetFileNameWithoutExtension(path)}</u></color></link>");

        static string Links(string[] names) => string.Join(", ", System.Array.ConvertAll(names, name => $"'{Link(AssetPath(name))}'"));

        static string CopyLine(string asset, string source, bool sourceIsDefaultProfile = false) =>
            SharedCopyLine(new[] { asset }, $"{asset}_VolumeProfile", source, sourceIsDefaultProfile);

        static string SharedCopyLine(string[] assets, string copy, string source, bool sourceIsDefaultProfile = false) =>
            $"{Links(assets)} now {(assets.Length == 1 ? "uses" : "use")} '{Link(AssetPath(copy))}', a copy of " +
            (sourceIsDefaultProfile ? "the default volume profile " : string.Empty) + $"'{Link(AssetPath(source))}'\\.";

        static string RemainingLine(string source, params string[] users) => $"'{Link(AssetPath(source))}' is still used by {Links(users)}\\.";

        static Regex CopyWarning(params string[] lines) => new Regex(string.Join("\n", lines));

        static int CountAssetsInFolder()
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { k_Folder }).Length;
        }

        // Version 16 lets only the shadow settings step run
        abstract class ShadowTestCase : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            protected UniversalRenderPipelineGlobalSettings m_GlobalSettings;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_GlobalSettings = globalSettingsAsset;
                globalSettingsAsset.m_AssetVersion = 16;
                CreateAssets();
            }

            public virtual void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                AssetDatabase.DeleteAsset(k_Folder);
            }

            protected abstract void CreateAssets();

            public abstract bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message);
        }

        class NonDefaultAssetWithoutProfile : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Asset;

            protected override void CreateAssets()
            {
                m_Asset = CreatePipelineAsset("NonDefault", 150f, 4, 0.1f);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Asset.volumeProfile == null || AssetDatabase.GetAssetPath(m_Asset.volumeProfile) != MigratedProfilePath("NonDefault"))
                {
                    message = $"Expected a new volume profile at {MigratedProfilePath("NonDefault")}, got '{AssetDatabase.GetAssetPath(m_Asset.volumeProfile)}'";
                    return false;
                }

                if (m_Asset.m_ShadowSettingsMigrationPending)
                {
                    message = "A migrated pipeline asset must be marked as migrated";
                    return false;
                }

                return HasMigratedValues(m_Asset.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class DefaultAsset : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Asset;

            protected override void CreateAssets()
            {
                m_Asset = CreateDefaultPipelineAsset("Default");
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Asset.volumeProfile != null || AssetDatabase.AssetPathExists(MigratedProfilePath("Default")))
                {
                    message = "A pipeline asset with default shadow settings must not get a volume profile";
                    return false;
                }

                message = "A pipeline asset with default shadow settings must still be marked as migrated, so that importing it again does not migrate it";
                return !m_Asset.m_ShadowSettingsMigrationPending;
            }
        }

        class SharedProfileWithDifferentValues : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_First;
            UniversalRenderPipelineAsset m_Second;
            VolumeProfile m_SharedProfile;

            protected override void CreateAssets()
            {
                m_SharedProfile = CreateProfileAsset("Shared");
                AddPersistedOverride<Bloom>(m_SharedProfile);

                m_First = CreatePipelineAsset("SharedA", 80f, 2, 0.15f, cascade2Split: 0.4f, profile: m_SharedProfile);
                m_Second = CreatePipelineAsset("SharedB", 120f, 4, 0.1f, profile: m_SharedProfile);

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("SharedA", "Shared"), CopyLine("SharedB", "Shared")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_SharedProfile.Has<ShadowsVolumeOverride>())
                {
                    message = "The shared volume profile must not get a Shadows override";
                    return false;
                }

                if (!UsesCopyWithBloom(m_First, out message) || !UsesCopyWithBloom(m_Second, out message))
                    return false;

                if (!HasMigratedValues(m_First.volumeProfile, 80f, 2, new Vector3(0.4f, k_Cascade4Split.y, k_Cascade4Split.z), 0.15f, out message))
                    return false;

                return HasMigratedValues(m_Second.volumeProfile, 120f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class SharedProfileWithDefaultAsset : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Default;
            UniversalRenderPipelineAsset m_NonDefault;
            VolumeProfile m_SharedProfile;

            protected override void CreateAssets()
            {
                m_SharedProfile = CreateProfileAsset("Shared");
                m_Default = CreateDefaultPipelineAsset("Mobile", m_SharedProfile);
                m_NonDefault = CreatePipelineAsset("PC", 120f, 4, 0.1f, profile: m_SharedProfile);

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("PC", "Shared"), RemainingLine("Shared", "Mobile")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Default.volumeProfile != m_SharedProfile || m_SharedProfile.Has<ShadowsVolumeOverride>())
                {
                    message = "A default pipeline asset must keep its shared volume profile without a Shadows override";
                    return false;
                }

                if (m_NonDefault.volumeProfile == m_SharedProfile)
                {
                    message = "A pipeline asset that shares its profile with a default one must use a copy";
                    return false;
                }

                return HasMigratedValues(m_NonDefault.volumeProfile, 120f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class OverrideAlreadyAuthored : ShadowTestCase
        {
            const float k_AuthoredDistance = 7f;

            UniversalRenderPipelineAsset m_Asset;
            VolumeProfile m_Profile;

            protected override void CreateAssets()
            {
                m_Profile = CreateProfileAsset("Authored");
                AddPersistedOverride<ShadowsVolumeOverride>(m_Profile).maxShadowDistance.value = k_AuthoredDistance;
                m_Asset = CreatePipelineAsset("WithAuthoredOverride", 150f, 4, 0.1f, profile: m_Profile);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Asset.volumeProfile != m_Profile || !m_Profile.TryGet(out ShadowsVolumeOverride shadows))
                {
                    message = "The pipeline asset lost its authored Shadows override";
                    return false;
                }

                message = $"An override that was already on the profile must keep its authored distance, got {shadows.maxShadowDistance.value}";
                return Mathf.Approximately(shadows.maxShadowDistance.value, k_AuthoredDistance);
            }
        }

        class SplitSetOfCascadeCount : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_TwoCascades;
            UniversalRenderPipelineAsset m_ThreeCascades;
            UniversalRenderPipelineAsset m_OneCascade;

            protected override void CreateAssets()
            {
                m_TwoCascades = CreatePipelineAsset("TwoCascades", 60f, 2, 0.2f, cascade2Split: 0.35f);
                m_ThreeCascades = CreatePipelineAsset("ThreeCascades", 60f, 3, 0.2f, cascade3Split: new Vector2(0.15f, 0.45f));
                m_OneCascade = CreatePipelineAsset("OneCascade", 60f, 1, 0.2f);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!HasMigratedValues(m_TwoCascades.volumeProfile, 60f, 2, new Vector3(0.35f, k_Cascade4Split.y, k_Cascade4Split.z), 0.2f, out message))
                {
                    message = $"Two cascades: {message}";
                    return false;
                }

                if (!HasMigratedValues(m_ThreeCascades.volumeProfile, 60f, 3, new Vector3(0.15f, 0.45f, k_Cascade4Split.z), 0.2f, out message))
                {
                    message = $"Three cascades: {message}";
                    return false;
                }

                if (!HasMigratedValues(m_OneCascade.volumeProfile, 60f, 1, k_Cascade4Split, 0.2f, out message))
                {
                    message = $"One cascade: {message}";
                    return false;
                }

                return true;
            }
        }

        class DefaultProfileAsQualityProfile : ShadowTestCase
        {
            URPDefaultVolumeProfileSettings m_Settings;
            VolumeProfile m_PreviousDefaultProfile;
            VolumeProfile m_DefaultProfile;
            UniversalRenderPipelineAsset m_DefaultAsset;
            UniversalRenderPipelineAsset m_NonDefaultAsset;

            protected override void CreateAssets()
            {
                m_DefaultProfile = CreateProfileAsset("DefaultProfile");
                AddPersistedOverride<Bloom>(m_DefaultProfile);

                m_Settings = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();
                Assert.IsNotNull(m_Settings, "The global settings have no default volume profile settings");
                m_PreviousDefaultProfile = m_Settings.volumeProfile;
                m_Settings.volumeProfile = m_DefaultProfile;

                m_DefaultAsset = CreateDefaultPipelineAsset("DefaultValues", m_DefaultProfile);
                m_NonDefaultAsset = CreatePipelineAsset("NonDefaultValues", 150f, 4, 0.1f, profile: m_DefaultProfile);

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("NonDefaultValues", "DefaultProfile", sourceIsDefaultProfile: true),
                    RemainingLine("DefaultProfile", "DefaultValues")));
            }

            public override void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Settings.volumeProfile = m_PreviousDefaultProfile;
                base.TearDown(globalSettingsAsset, renderPipelineAsset);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_DefaultProfile.Has<ShadowsVolumeOverride>())
                {
                    message = "The default volume profile must not get a Shadows override, because every quality level inherits it";
                    return false;
                }

                if (m_DefaultAsset.volumeProfile != m_DefaultProfile)
                {
                    message = "A pipeline asset with default shadow settings must keep the default volume profile as its quality profile";
                    return false;
                }

                if (!UsesCopyWithBloom(m_NonDefaultAsset, out message))
                    return false;

                return HasMigratedValues(m_NonDefaultAsset.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class MigratingTwice : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_First;
            UniversalRenderPipelineAsset m_Second;

            protected override void CreateAssets()
            {
                var shared = CreateProfileAsset("Shared");
                m_First = CreatePipelineAsset("SharedA", 80f, 2, 0.15f, profile: shared);
                m_Second = CreatePipelineAsset("SharedB", 120f, 4, 0.1f, profile: shared);

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("SharedA", "Shared"), CopyLine("SharedB", "Shared")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                int assetCount = CountAssetsInFolder();
                var firstProfile = m_First.volumeProfile;
                var secondProfile = m_Second.volumeProfile;

                m_GlobalSettings.m_AssetVersion = 16;
                UniversalRenderPipelineGlobalSettings.UpgradeAsset(m_GlobalSettings.GetEntityId());

                if (CountAssetsInFolder() != assetCount || m_First.volumeProfile != firstProfile || m_Second.volumeProfile != secondProfile)
                {
                    message = "Running the migration again must not create or reassign volume profiles";
                    return false;
                }

                if (firstProfile.components.Count != 1 || secondProfile.components.Count != 1)
                {
                    message = $"Running the migration again must not add overrides, got {firstProfile.components.Count} and {secondProfile.components.Count}";
                    return false;
                }

                message = string.Empty;
                return true;
            }
        }

        internal class NotEditablePaths : AssetModificationProcessor
        {
            internal static readonly HashSet<string> s_Paths = new HashSet<string>();

            static bool MakeEditable(string[] paths, string prompt, List<string> outNotEditablePaths)
            {
                bool editable = true;
                foreach (var path in paths)
                {
                    if (!s_Paths.Contains(path))
                        continue;

                    outNotEditablePaths.Add(path);
                    editable = false;
                }
                return editable;
            }
        }

        class NotEditableProfile : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Asset;
            VolumeProfile m_Profile;

            protected override void CreateAssets()
            {
                m_Profile = CreateProfileAsset("Locked");
                m_Asset = CreatePipelineAsset("WithLockedProfile", 150f, 4, 0.1f, profile: m_Profile);
                NotEditablePaths.s_Paths.Add(AssetPath("Locked"));

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("WithLockedProfile", "Locked")));
            }

            public override void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                NotEditablePaths.s_Paths.Clear();
                base.TearDown(globalSettingsAsset, renderPipelineAsset);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Profile.Has<ShadowsVolumeOverride>())
                {
                    message = "The locked volume profile must stay unchanged";
                    return false;
                }

                if (m_Asset.m_ShadowSettingsMigrationPending)
                {
                    message = "A pipeline asset whose volume profile is locked must still migrate to a copy";
                    return false;
                }

                return HasMigratedValues(m_Asset.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class NotEditableAsset : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Asset;

            protected override void CreateAssets()
            {
                m_Asset = CreatePipelineAsset("Locked", 150f, 4, 0.1f);
                NotEditablePaths.s_Paths.Add(AssetPath("Locked"));

                LogAssert.Expect(LogType.Warning, new Regex($"'{Link(AssetPath("Locked"))}' is not editable"));
            }

            public override void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                NotEditablePaths.s_Paths.Clear();
                base.TearDown(globalSettingsAsset, renderPipelineAsset);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Asset.volumeProfile != null || AssetDatabase.AssetPathExists(MigratedProfilePath("Locked")))
                {
                    message = "A pipeline asset that is not editable must not get a volume profile";
                    return false;
                }

                message = "A pipeline asset that is not editable must stay pending, so that a later migration can retry";
                return m_Asset.m_ShadowSettingsMigrationPending;
            }
        }

        class ProfileOfOneAsset : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Asset;
            VolumeProfile m_Profile;

            protected override void CreateAssets()
            {
                m_Profile = CreateProfileAsset("Own");
                AddPersistedOverride<Bloom>(m_Profile);
                m_Asset = CreatePipelineAsset("WithOwnProfile", 150f, 4, 0.1f, profile: m_Profile);

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("WithOwnProfile", "Own")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Profile.Has<ShadowsVolumeOverride>())
                {
                    message = "Scenes and prefabs can also use the volume profile, so it must not get a Shadows override";
                    return false;
                }

                if (!UsesCopyWithBloom(m_Asset, out message))
                    return false;

                return HasMigratedValues(m_Asset.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class SharedProfileWithSameValues : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_First;
            UniversalRenderPipelineAsset m_Second;
            VolumeProfile m_SharedProfile;

            protected override void CreateAssets()
            {
                m_SharedProfile = CreateProfileAsset("Shared");
                AddPersistedOverride<Bloom>(m_SharedProfile);

                m_First = CreatePipelineAsset("SameA", 120f, 4, 0.1f, profile: m_SharedProfile);
                m_Second = CreatePipelineAsset("SameB", 120f, 4, 0.1f, profile: m_SharedProfile);

                LogAssert.Expect(LogType.Warning, CopyWarning("share one new profile\\.", SharedCopyLine(new[] { "SameA", "SameB" }, "Shared_Shadows", "Shared")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_SharedProfile.Has<ShadowsVolumeOverride>())
                {
                    message = "The shared volume profile must not get a Shadows override";
                    return false;
                }

                if (m_First.volumeProfile != m_Second.volumeProfile)
                {
                    message = "Pipeline assets with the same volume profile and shadow settings must share one copy";
                    return false;
                }

                if (!UsesCopyWithBloom(m_First, AssetPath("Shared_Shadows"), out message))
                    return false;

                return HasMigratedValues(m_First.volumeProfile, 120f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class SharedProfileWithMixedValues : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_SameA;
            UniversalRenderPipelineAsset m_SameB;
            UniversalRenderPipelineAsset m_Other;
            UniversalRenderPipelineAsset m_Default;
            VolumeProfile m_SharedProfile;

            protected override void CreateAssets()
            {
                m_SharedProfile = CreateProfileAsset("Shared");
                AddPersistedOverride<Bloom>(m_SharedProfile);

                m_SameA = CreatePipelineAsset("MixedA", 120f, 4, 0.1f, profile: m_SharedProfile);
                m_SameB = CreatePipelineAsset("MixedB", 120f, 4, 0.1f, profile: m_SharedProfile);
                m_Other = CreatePipelineAsset("MixedC", 80f, 2, 0.15f, profile: m_SharedProfile);
                m_Default = CreateDefaultPipelineAsset("MixedD", m_SharedProfile);

                LogAssert.Expect(LogType.Warning, CopyWarning(
                    SharedCopyLine(new[] { "MixedA", "MixedB" }, "Shared_Shadows", "Shared"),
                    CopyLine("MixedC", "Shared"),
                    RemainingLine("Shared", "MixedD")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Default.volumeProfile != m_SharedProfile || m_SharedProfile.Has<ShadowsVolumeOverride>())
                {
                    message = "A default pipeline asset must keep the shared volume profile without a Shadows override";
                    return false;
                }

                if (m_SameA.volumeProfile != m_SameB.volumeProfile || !UsesCopyWithBloom(m_SameA, AssetPath("Shared_Shadows"), out message))
                {
                    message = "Pipeline assets with the same shadow settings must share one copy";
                    return false;
                }

                if (!UsesCopyWithBloom(m_Other, out message))
                    return false;

                if (!HasMigratedValues(m_SameA.volumeProfile, 120f, 4, k_Cascade4Split, 0.1f, out message))
                    return false;

                return HasMigratedValues(m_Other.volumeProfile, 80f, 2, new Vector3(0.25f, k_Cascade4Split.y, k_Cascade4Split.z), 0.15f, out message);
            }
        }

        class SameValuesWithoutProfile : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_First;
            UniversalRenderPipelineAsset m_Second;

            protected override void CreateAssets()
            {
                m_First = CreatePipelineAsset("NoProfileA", 150f, 4, 0.1f);
                m_Second = CreatePipelineAsset("NoProfileB", 150f, 4, 0.1f);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (AssetDatabase.GetAssetPath(m_First.volumeProfile) != MigratedProfilePath("NoProfileA")
                    || AssetDatabase.GetAssetPath(m_Second.volumeProfile) != MigratedProfilePath("NoProfileB"))
                {
                    message = "Pipeline assets without a volume profile must each get their own profile, even with the same shadow settings";
                    return false;
                }

                return HasMigratedValues(m_First.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message)
                    && HasMigratedValues(m_Second.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class SameValuesWithDifferentProfiles : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_First;
            UniversalRenderPipelineAsset m_Second;

            protected override void CreateAssets()
            {
                m_First = CreatePipelineAsset("OnProfileA", 150f, 4, 0.1f, profile: CreateProfileAsset("ProfileA"));
                m_Second = CreatePipelineAsset("OnProfileB", 150f, 4, 0.1f, profile: CreateProfileAsset("ProfileB"));

                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("OnProfileA", "ProfileA"), CopyLine("OnProfileB", "ProfileB")));
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (AssetDatabase.GetAssetPath(m_First.volumeProfile) != MigratedProfilePath("OnProfileA")
                    || AssetDatabase.GetAssetPath(m_Second.volumeProfile) != MigratedProfilePath("OnProfileB"))
                {
                    message = "Pipeline assets with different volume profiles must get their own copies, even with the same shadow settings";
                    return false;
                }

                return HasMigratedValues(m_First.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message)
                    && HasMigratedValues(m_Second.volumeProfile, 150f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class NotEditableAssetWithSameValues : ShadowTestCase
        {
            UniversalRenderPipelineAsset m_Locked;
            UniversalRenderPipelineAsset m_Editable;
            VolumeProfile m_SharedProfile;

            protected override void CreateAssets()
            {
                m_SharedProfile = CreateProfileAsset("Shared");
                m_Locked = CreatePipelineAsset("GroupA", 120f, 4, 0.1f, profile: m_SharedProfile);
                m_Editable = CreatePipelineAsset("GroupB", 120f, 4, 0.1f, profile: m_SharedProfile);
                NotEditablePaths.s_Paths.Add(AssetPath("GroupA"));

                LogAssert.Expect(LogType.Warning, new Regex($"'{Link(AssetPath("GroupA"))}' is not editable"));
                LogAssert.Expect(LogType.Warning, CopyWarning(CopyLine("GroupB", "Shared"), RemainingLine("Shared", "GroupA")));
            }

            public override void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                NotEditablePaths.s_Paths.Clear();
                base.TearDown(globalSettingsAsset, renderPipelineAsset);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_Locked.volumeProfile != m_SharedProfile || !m_Locked.m_ShadowSettingsMigrationPending)
                {
                    message = "A pipeline asset that is not editable must keep its volume profile and stay pending";
                    return false;
                }

                if (AssetDatabase.GetAssetPath(m_Editable.volumeProfile) != MigratedProfilePath("GroupB"))
                {
                    message = $"The editable pipeline asset must use a copy named after itself, got '{AssetDatabase.GetAssetPath(m_Editable.volumeProfile)}'";
                    return false;
                }

                return HasMigratedValues(m_Editable.volumeProfile, 120f, 4, k_Cascade4Split, 0.1f, out message);
            }
        }

        class AssetInReadOnlyPackage : ShadowTestCase
        {
            System.Predicate<string> m_PreviousIsInReadOnlyPackage;
            UniversalRenderPipelineAsset m_InPackage;
            UniversalRenderPipelineAsset m_InProject;

            protected override void CreateAssets()
            {
                m_InPackage = CreatePipelineAsset("InReadOnlyPackage", 150f, 4, 0.1f);
                m_InProject = CreatePipelineAsset("InProject", 80f, 2, 0.15f);

                m_PreviousIsInReadOnlyPackage = UniversalRenderPipelineGlobalSettings.isInReadOnlyPackage;
                string packagePath = AssetPath("InReadOnlyPackage");
                UniversalRenderPipelineGlobalSettings.isInReadOnlyPackage = path => path == packagePath;

                LogAssert.Expect(LogType.Warning, new Regex($"'{Link(packagePath)}' is in a read-only package"));
            }

            public override void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                UniversalRenderPipelineGlobalSettings.isInReadOnlyPackage = m_PreviousIsInReadOnlyPackage;
                base.TearDown(globalSettingsAsset, renderPipelineAsset);
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (m_InPackage.volumeProfile != null || AssetDatabase.AssetPathExists(MigratedProfilePath("InReadOnlyPackage")))
                {
                    message = "A pipeline asset in a read-only package must not get a volume profile";
                    return false;
                }

                if (!m_InPackage.m_ShadowSettingsMigrationPending)
                {
                    message = "A pipeline asset in a read-only package must stay pending";
                    return false;
                }

                return HasMigratedValues(m_InProject.volumeProfile, 80f, 2, new Vector3(0.25f, k_Cascade4Split.y, k_Cascade4Split.z), 0.15f, out message);
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new NonDefaultAssetWithoutProfile())
                .SetName("When a pipeline asset with custom shadow settings has no volume profile, a profile with the Shadows override is created next to it"),
            new TestCaseData(new DefaultAsset())
                .SetName("When a pipeline asset has default shadow settings, no volume profile is created"),
            new TestCaseData(new SharedProfileWithDifferentValues())
                .SetName("When two pipeline assets share a volume profile with different shadow settings, each gets its own copy and the shared profile is unchanged"),
            new TestCaseData(new SharedProfileWithDefaultAsset())
                .SetName("When a default pipeline asset shares its volume profile, the other pipeline asset gets a copy"),
            new TestCaseData(new OverrideAlreadyAuthored())
                .SetName("When the volume profile already has a Shadows override, the migration leaves it untouched"),
            new TestCaseData(new SplitSetOfCascadeCount())
                .SetName("When migrating, the cascade splits come from the split set of the cascade count"),
            new TestCaseData(new DefaultProfileAsQualityProfile())
                .SetName("When a pipeline asset uses the default volume profile as its quality profile, it gets a copy and the default profile gets no Shadows override"),
            new TestCaseData(new MigratingTwice())
                .SetName("When the migration runs twice, the second run changes nothing"),
            new TestCaseData(new NotEditableProfile())
                .SetName("When the volume profile is not editable, the pipeline asset still migrates to a copy and the profile is unchanged"),
            new TestCaseData(new NotEditableAsset())
                .SetName("When the pipeline asset is not editable, it stays pending and gets no volume profile"),
            new TestCaseData(new ProfileOfOneAsset())
                .SetName("When a pipeline asset with custom shadow settings has its own volume profile, it gets a copy and the original profile is unchanged"),
            new TestCaseData(new SharedProfileWithSameValues())
                .SetName("When pipeline assets share a volume profile and have the same shadow settings, they share one copy and the profile is unchanged"),
            new TestCaseData(new SharedProfileWithMixedValues())
                .SetName("When pipeline assets share a volume profile, the ones with the same shadow settings share a copy, the others get their own and default ones keep the profile"),
            new TestCaseData(new SameValuesWithoutProfile())
                .SetName("When pipeline assets without a volume profile have the same shadow settings, each gets its own new profile"),
            new TestCaseData(new SameValuesWithDifferentProfiles())
                .SetName("When pipeline assets with the same shadow settings use different volume profiles, each gets a copy of its own profile"),
            new TestCaseData(new NotEditableAssetWithSameValues())
                .SetName("When one of two pipeline assets with the same profile and shadow settings is not editable, the other gets its own copy and the locked one stays pending"),
            new TestCaseData(new AssetInReadOnlyPackage())
                .SetName("When a pipeline asset with custom shadow settings is in a read-only package, it is skipped with a warning and other assets still migrate"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
