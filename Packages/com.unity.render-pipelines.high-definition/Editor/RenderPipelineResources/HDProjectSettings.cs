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
    sealed class HDProjectSettings : HDProjectSettingsReadOnlyBase, IVersionable<HDProjectSettings.Version>
    {
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

        public static new string projectSettingsFolderPath
        {
            get => instance.m_ProjectSettingFolderPath;
            set
            {
                instance.m_ProjectSettingFolderPath = value;
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
        static HDProjectSettings instance
        {
            get
            {
                //In case we are early loading it through HDProjectSettingsReadOnlyBase, migration can have not been done.
                //To not create an infinite callstack loop through "instance", destroy it to force reloading it.
                //(migration is done at loading time)
                if (s_Instance != null && (!(s_Instance is HDProjectSettings inst) || inst.m_Version != MigrationDescription.LastVersion<Version>()))
                {
                    if (!(s_Instance is HDProjectSettings))
                        Debug.Log($"Not a HDProjectSettings: {s_Instance?.GetType()?.ToString() ?? "null" }");
                    else
                        Debug.Log($"Version: {(s_Instance as HDProjectSettings).m_Version}");
                    DestroyImmediate(s_Instance);
                    s_Instance = null;
                }

                return s_Instance as HDProjectSettings ?? CreateOrLoad();
            }
        }

        //// We force the instance to be loaded/created and ready with valid values on assembly reload.
        [InitializeOnLoadMethod]
        static void InitializeFillPresentPluginMaterialVersionsOnLoad()
        {
            // Make sure the cached last seen plugin versions (capped to codebase versions) and their sum is valid
            // on assembly reload.
            instance.FillPresentPluginMaterialVersions();
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

        //Note: if created from HDProjectSettingsReadOnlyBase, it can be loaded fully and such as a HDProjectSettings
        //Thus it is not loaded again and we need to ensure the migration is done when accessing data too (see instance).
        //Never use "instance" here as it can create infinite call loop. Use s_Instance instead.
        static HDProjectSettings CreateOrLoad()
        {
            //try load: if it exists, this will trigger the call to the private ctor
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            HDProjectSettings inst = s_Instance as HDProjectSettings;

            //else create
            if (inst == null)
            {
                HDProjectSettings created = CreateInstance<HDProjectSettings>();
                created.hideFlags = HideFlags.HideAndDontSave;
                inst = s_Instance as HDProjectSettings;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
            inst.FillPresentPluginMaterialVersions();

            if (k_Migration.Migrate(inst))
                Save();

            return inst;
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

        // Migration Case 1:
        //   - No early migration occured, we need to access one field here and this pass by a static accessor such as HDProjectSettings.materialVersionForUpgrade
        //   - This will request the instance which is not loaded/created yet. So the LoadOrCreate will be called that should trigger the migration.
        //
        // Migration Case 2:
        //   - When opening unity Editor, the package.json have changed and request a newer version of the HDRP package.
        //     While evreything is reimported by the ADB, the render loop will start to be called (when modal say "Load Scene" in fact).
        //  - This can trigger a migration from former system without GlobalSettings to the new one. In this case we will try to create a
        //    HDRenderPipelineGlobalSettings out of the HDRenderPipelineAsset in the GraphicSettings. For this asset to be created, we must
        //    access the m_ProjectSettingFolderPath. This is done from the RuntimeAssembly and we have no certitude that the editor assembly
        //    is loaded. This is the reason we have a base class HDProjectSettingsReadonlyBase that lives in Runtime assembly.
        //  - Though HDProjectSettingsReadonlyBase don't see every part of the object and can just try to load it. When this is the case,
        //    the migration is NOT triggered.
        //  - So next time we will need access to HDProjectSettings, we need to say "Hey migrate if you need".
        //  - For this reason, in the instance, we need to check if we need to migrate or not. This is done by checking the version and remove
        //    the static instance in this case to force its reloading in the HDProjectSettings way (which means with Migration)
        //
        // When migration is triggered:
        //  - When we call the Migrate(), the System will try first to check the version to see if migration is needed. So if IVersionable.version
        //    use instance, we will suppress the static instance to reload it if the version is not last (see Case 2). In the end, as it is loaded
        //    again, it will request a Migration. This is a recursive call that will lead to stack overflow. So we must not use instance in IVersionable.version
        //  - Then if version is not the last, we will execute missing migration steps. Once again if inside a step we use instance we will cause
        //    a recursive call like in above and will produce a stack overflow. So no HDProjectSettings.materialVersionForUpgrade but we can safely
        //    use (s_Instance as HDProjectSettings).m_LastMaterialVersion or the given data: data.m_LastMaterialVersion.

        // NEVER USE "instance" HERE as it can create infinite call loop. Use s_Instance instead. (see comment above)
        Version IVersionable<Version>.version { get => (s_Instance as HDProjectSettings).m_Version; set => (s_Instance as HDProjectSettings).m_Version = value; }

        // NEVER USE "instance" HERE as it can create infinite call loop. Use s_Instance or data instead. (see comment above)
        static readonly MigrationDescription<Version, HDProjectSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SplittedWithHDUserSettings, (HDProjectSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                HDUserSettings.wizardPopupAlreadyShownOnce = data.m_ObsoleteWizardPopupAlreadyShownOnce;
                HDUserSettings.wizardNeedRestartAfterChangingToDX12 = data.m_ObsoleteWizardNeedRestartAfterChangingToDX12;
                HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload = data.m_ObsoleteWizardNeedToRunFixAllAgainAfterDomainReload;
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
