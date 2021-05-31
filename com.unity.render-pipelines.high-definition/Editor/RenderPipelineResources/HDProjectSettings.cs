using UnityEditorInternal;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    //As ScriptableSingleton is not usable due to internal FilePathAttribute,
    //copying mechanism here
    class HDProjectSettings : ScriptableObject, IVersionable<HDProjectSettings.Version>
    {
        const string filePath = "ProjectSettings/HDRPProjectSettings.asset";

        [SerializeField]
        string m_ProjectSettingFolderPath = "HDRPDefaultResources";
        [SerializeField]
        bool m_WizardPopupAtStart = true;

        // Note: this is for the *material version*, which uses MaterialPostProcessor k_Migrations[] as migration
        // functions table. See note on Version enum above.
        [SerializeField]
        int m_LastMaterialVersion = k_NeverProcessedMaterialVersion;

        [SerializeField]
        ShaderGraphVersion m_HDShaderGraphLastSeenVersion = ShaderGraphVersion.NeverMigrated;

        // This tracks latest versions of every plugin subtargets ever seen in the project.
        // The dictionary key is the SubTarget GUID.
        // If the project has ever seen an external-to-HDRP ("plugin") material, it saves the version
        // of the material here. That way, we can use the same logic to forward upgrade calls
        // than for the internally known HDRP materials: if a plugin material version is higher than
        // what is seen here, we can trigger a re-import (see RegisterUpgraderReimport in MaterialPostProcessor).
        [SerializeField]
        PluginMaterialVersions m_PluginMaterialVersions = new PluginMaterialVersions();

        // Same as above but for the *subtarget* versioning specific to the subtarget (ie not HDRP ShaderGraphVersion,
        // see m_HDShaderGraphLastSeenVersion above for that).
        [SerializeField]
        PluginMaterialVersions m_PluginSubTargetVersions = new PluginMaterialVersions();

        // The following filters m_PluginMaterialVersions on load/save based on plugin subtargets
        // with the code base actually still present in the project.
        // Unused for now as we can't filter materials per active shader type so we reimport all materials
        // at the same time.
        [NonSerialized]
        Dictionary<GUID, (int, int)> m_InProjectPluginLastSeenMaterialAndSubTargetVersions = new Dictionary<GUID, (int, int)>();

        /// <summary>
        /// This is to quickly decide if some materials depending on plugin subtargets
        /// (that have actually their plugin code present in the project) might need upgrade.
        /// See pluginSubTargetMaterialVersionSum in HDShaderUtils: summed versions in here
        /// should never be allowed to be higher than that returned by the currently present
        /// (in project) plugin SubTarget codebase - ie that returned by
        /// <by cref="IPluginSubTargetMaterialUtils"/>.
        /// <See also cref="MaterialReimporter.RegisterUpgraderReimport"/>
        /// </summary>
        [NonSerialized]
        long m_InProjectPluginLastSeenMaterialVersionsSum = (int)PluginMaterial.GenericVersions.NeverMigrated;

        [NonSerialized]
        long m_InProjectPluginLastSeenSubTargetVersionsSum = (int)PluginMaterial.GenericVersions.NeverMigrated;

        internal const int k_NeverProcessedMaterialVersion = -1;

        public static string projectSettingsFolderPath
        {
            get => instance.m_ProjectSettingFolderPath;
            set
            {
                instance.m_ProjectSettingFolderPath = value;
                Save();
            }
        }

        public static bool wizardIsStartPopup
        {
            get => instance.m_WizardPopupAtStart;
            set
            {
                instance.m_WizardPopupAtStart = value;
                Save();
            }
        }


        public static int materialVersionForUpgrade
        {
            get => instance.m_LastMaterialVersion;
            set
            {
                instance.m_LastMaterialVersion = value;
                Save();
            }
        }

        public static ShaderGraphVersion hdShaderGraphLastSeenVersion
        {
            get => instance.m_HDShaderGraphLastSeenVersion;
            set
            {
                instance.m_HDShaderGraphLastSeenVersion = value;
                Save();
            }
        }

        public static long pluginSubTargetLastSeenMaterialVersionsSum
        {
            get => instance.m_InProjectPluginLastSeenMaterialVersionsSum;
        }

        public static long pluginSubTargetLastSeenSubTargetVersionsSum
        {
            get => instance.m_InProjectPluginLastSeenSubTargetVersionsSum;
        }

        //singleton pattern
        static HDProjectSettings s_Instance;
        static HDProjectSettings instance => s_Instance ?? CreateOrLoad();

        HDProjectSettings()
        {
            s_Instance = this;
            // s_Instance.FillPresentPluginMaterialVersions(); <
            // We can't call this here as the scriptable object will not be deserialized in yet
        }

        // We force the instance to be loaded/created and ready with valid values on assembly reload.
        // We also use this so that the HDUserSettings.cs on the runtime assembly will have the HDProjectSettings proxy
        // injected before it is used.
        [InitializeOnLoadMethod]
        static void Reset()
        {
            // Make sure the cached last seen plugin versions (capped to codebase versions) and their sum is valid
            // on assembly reload.
            instance.FillPresentPluginMaterialVersions();
            HDProjectSettingsProxy.Init(() => projectSettingsFolderPath);
        }

        void FillPresentPluginMaterialVersions()
        {
            //m_InProjectPluginLastSeenMaterialVersions.Clear();
            m_InProjectPluginLastSeenMaterialAndSubTargetVersions.Clear();
            m_InProjectPluginLastSeenMaterialVersionsSum = 0;
            m_InProjectPluginLastSeenSubTargetVersionsSum = 0;

            int pluginSubTargetsSeenInHDProjectSettings = 0;

            foreach (var entry in m_PluginMaterialVersions)
            {
                if (!m_PluginSubTargetVersions.TryGetValue(entry.Key, out int subTargetVer))
                {
                    subTargetVer = (int)PluginMaterial.GenericVersions.NeverMigrated;
                }
                else
                {
                    pluginSubTargetsSeenInHDProjectSettings++;
                }

                if (HDShaderUtils.GetMaterialPluginSubTarget(entry.Key, out IPluginSubTargetMaterialUtils subTarget))
                {
                    try
                    {
                        //
                        // We clamp from above the last seen version saved in HDRP project settings by the latest
                        // known version of the code base as a precaution so we don't constantly try to upgrade
                        // potentially newer materials while the code base was seemingly downgraded (an unsupported
                        // scenario anyway).
                        // Also, the premise that all versions bookkept here are *each* lower or equal to the corresponding
                        // code base version is necessary for the plugin version check to be simplified to just checking
                        // the sum of all possible plugin versions present.
                        int matVer = Math.Min(subTarget.latestMaterialVersion, entry.Value);
                        subTargetVer = Math.Min(subTarget.latestSubTargetVersion, subTargetVer);
                        m_InProjectPluginLastSeenMaterialAndSubTargetVersions.Add(entry.Key, (matVer, subTargetVer));
                        m_InProjectPluginLastSeenMaterialVersionsSum += matVer;
                        m_InProjectPluginLastSeenSubTargetVersionsSum += subTargetVer;
                    }
                    catch
                    {
                        Debug.LogError("Malformed HDProjectSettings.asset: duplicate plugin SubTarget GUID");
                    }
                }
            }

            if (m_InProjectPluginLastSeenMaterialAndSubTargetVersions.Count == 0)
            {
                // This means that either we don't have any plugin SubTarget in the project or HDProjectSettings indicates
                // we never saw any one of them yet. Since IPluginSubTargetMaterialUtils.latestMaterialVersion can never
                // be == 0 (never migrated version), we make sure the importer will pick up the materials to be patched at least
                // for the initial migration: we set the "seen material versions" sum to a value that the code base latestversions
                // sum will always exceed:
                m_InProjectPluginLastSeenMaterialVersionsSum = (int)PluginMaterial.GenericVersions.NeverMigrated;
            }
            if (pluginSubTargetsSeenInHDProjectSettings == 0)
            {
                m_InProjectPluginLastSeenSubTargetVersionsSum = (int)PluginMaterial.GenericVersions.NeverMigrated;
            }
        }

        public static void UpdateLastSeenMaterialVersionsOfPluginSubTargets()
        {
            var allPluginSubTargets = HDShaderUtils.GetHDPluginSubTargets();

            foreach (var entry in allPluginSubTargets)
            {
                var subTarget = entry.Value;
                if (instance.m_PluginMaterialVersions.TryGetValue(entry.Key, out int lastSeenVersion))
                {
                    if (subTarget.latestMaterialVersion > lastSeenVersion)
                    {
                        instance.m_PluginMaterialVersions[entry.Key] = subTarget.latestMaterialVersion;
                    }
                    // else SubTarget plugin downgraded or same version, nothing to do
                }
                else
                {
                    // It's the first time this HD project has seen this plugin SubTarget, save the
                    // last seen material version for this SubTarget GUID:
                    instance.m_PluginMaterialVersions.Add(entry.Key, subTarget.latestMaterialVersion);
                }
            }

            instance.FillPresentPluginMaterialVersions();
            Save();
        }

        public static void UpdateLastSeenSubTargetVersionsOfPluginSubTargets()
        {
            var allPluginSubTargets = HDShaderUtils.GetHDPluginSubTargets();

            foreach (var entry in allPluginSubTargets)
            {
                var subTarget = entry.Value;
                if (instance.m_PluginSubTargetVersions.TryGetValue(entry.Key, out int lastSeenVersion))
                {
                    if (subTarget.latestSubTargetVersion > lastSeenVersion)
                    {
                        instance.m_PluginSubTargetVersions[entry.Key] = subTarget.latestSubTargetVersion;
                    }
                    // else SubTarget plugin downgraded or same version, nothing to do
                }
                else
                {
                    // It's the first time this HD project (the material post processor)
                    // has seen this plugin SubTarget in a HDRP ShaderGraph import scan triggered,
                    // save the last seen *subtarget* version for this SubTarget GUID:
                    instance.m_PluginSubTargetVersions.Add(entry.Key, subTarget.latestSubTargetVersion);
                }
            }

            instance.FillPresentPluginMaterialVersions();
            Save();
        }

        static HDProjectSettings CreateOrLoad()
        {
            //try load: if it exists, this will trigger the call to the private ctor
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            //else create
            if (s_Instance == null)
            {
                HDProjectSettings created = CreateInstance<HDProjectSettings>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
            s_Instance.FillPresentPluginMaterialVersions();

            if (k_Migration.Migrate(s_Instance))
                Save();

            return s_Instance;
        }

        static void Save()
        {
            if (s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_Instance }, filePath, allowTextSerialization: true);
        }

        #region Migration
        // Note that k_Migrations in MaterialPostProcessor was started with a valid migration step at k_Migrations[0],
        // that's why we use k_NeverProcessedMaterialVersion = -1. We also dont use IVersionable (see MigrationDescription.cs) for it.
        // But when using the later generic versionable framework, it is better to use 0 as a place holder for a never migrated version
        // (and thus a step never to be executed for that enum entry) because underlying enum values are ordered as unsigned and
        // MigrationDescription.LastVersion<Version>() will not work properly - ie it can return -1 if present instead of other positive values.
        // (as the enum symbol with -1 will be listed as the last enum values in UnityEngine.Rendering.HighDefinition.TypeInfo.GetEnumLastValue<T>())
        internal enum Version
        {
            None,
            First,
            SplittedWithHDUserSettings
        }
#pragma warning disable 414 // never used
        [SerializeField, FormerlySerializedAs("version")]
        Version m_Version = MigrationDescription.LastVersion<Version>();
#pragma warning restore 414

        Version IVersionable<Version>.version { get => instance.m_Version; set => instance.m_Version = value; }

        static readonly MigrationDescription<Version, HDProjectSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SplittedWithHDUserSettings, (HDProjectSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                HDUserSettings.wizardPopupAlreadyShownOnce = instance.m_ObsoleteWizardPopupAlreadyShownOnce;
                HDUserSettings.wizardActiveTab = instance.m_ObsoleteWizardActiveTab;
                HDUserSettings.wizardNeedRestartAfterChangingToDX12 = instance.m_ObsoleteWizardNeedRestartAfterChangingToDX12;
                HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload = instance.m_ObsoleteWizardNeedToRunFixAllAgainAfterDomainReload;
#pragma warning restore 618 // Type or member is obsolete
            })
        );

#pragma warning disable 649 // Field never assigned
        [SerializeField, Obsolete("Moved from HDProjectSettings to HDUserSettings"), FormerlySerializedAs("m_WizardPopupAlreadyShownOnce")]
        bool m_ObsoleteWizardPopupAlreadyShownOnce;
        [SerializeField, Obsolete("Moved from HDProjectSettings to HDUserSettings"), FormerlySerializedAs("m_WizardActiveTab")]
        int m_ObsoleteWizardActiveTab;
        [SerializeField, Obsolete("Moved from HDProjectSettings to HDUserSettings"), FormerlySerializedAs("m_WizardNeedRestartAfterChangingToDX12")]
        bool m_ObsoleteWizardNeedRestartAfterChangingToDX12;
        [SerializeField, Obsolete("Moved from HDProjectSettings to HDUserSettings"), FormerlySerializedAs("m_WizardNeedToRunFixAllAgainAfterDomainReload")]
        bool m_ObsoleteWizardNeedToRunFixAllAgainAfterDomainReload;
#pragma warning restore 649 // Field never assigned
        #endregion
    }
}
