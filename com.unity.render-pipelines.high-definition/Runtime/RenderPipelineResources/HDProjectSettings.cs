#if UNITY_EDITOR
using UnityEditorInternal;
using System.IO;
using System;
using UnityEngine.Serialization;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    //As ScriptableSingleton is not usable due to internal FilePathAttribute,
    //copying mechanism here
    class HDProjectSettings : ScriptableObject, IVersionable<HDProjectSettings.Version>
    {
        const string filePath = "ProjectSettings/HDRPProjectSettings.asset";

        [SerializeField]
        string m_ProjectSettingFolderPath = "HDRPDefaultResources";
        [SerializeField]
        int m_LastMaterialVersion = k_NeverProcessedMaterialVersion;

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

        public static int materialVersionForUpgrade
        {
            get => instance.m_LastMaterialVersion;
            set
            {
                instance.m_LastMaterialVersion = value;
                Save();
            }
        }

        //singleton pattern
        static HDProjectSettings s_Instance;
        static HDProjectSettings instance => s_Instance ?? CreateOrLoad();

        HDProjectSettings()
        {
            s_Instance = this;
        }

        static HDProjectSettings CreateOrLoad()
        {
            //try load
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            //else create
            if (s_Instance == null)
            {
                HDProjectSettings created = CreateInstance<HDProjectSettings>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);

            if (k_Migration.Migrate(instance))
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
                HDUserSettings.wizardIsStartPopup = instance.m_ObsoleteWizardPopupAtStart;
                HDUserSettings.wizardPopupAlreadyShownOnce = instance.m_ObsoleteWizardPopupAlreadyShownOnce;
                HDUserSettings.wizardActiveTab = instance.m_ObsoleteWizardActiveTab;
                HDUserSettings.wizardNeedRestartAfterChangingToDX12 = instance.m_ObsoleteWizardNeedRestartAfterChangingToDX12;
                HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload = instance.m_ObsoleteWizardNeedToRunFixAllAgainAfterDomainReload;
#pragma warning restore 618 // Type or member is obsolete
            })
        );

#pragma warning disable 649 // Field never assigned
        [SerializeField, Obsolete("Moved from HDProjectSettings to HDUserSettings"), FormerlySerializedAs("m_WizardPopupAtStart")]
        bool m_ObsoleteWizardPopupAtStart;
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
#endif
}
