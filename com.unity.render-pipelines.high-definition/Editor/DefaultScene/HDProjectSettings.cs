using UnityEngine;
using UnityEditorInternal;
using System.IO;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    //As ScriptableSingleton is not usable due to internal FilePathAttribute,
    //copying mechanism here
    internal class HDProjectSettings : ScriptableObject
    {
        const string filePath = "ProjectSettings/HDRPProjectSettings.asset";

        //preparing to eventual migration later
        enum Version
        {
            None,
            First
        }
#pragma warning disable 414 // never used
        [SerializeField]
        Version version = MigrationDescription.LastVersion<Version>();
#pragma warning restore 414

        [SerializeField]
        GameObject m_DefaultScenePrefabSaved;
        [SerializeField]
        GameObject m_DefaultDXRScenePrefabSaved;
        [SerializeField]
        string m_ProjectSettingFolderPath = "HDRPDefaultResources";
        [SerializeField]
        bool m_WizardPopupAtStart = true;
        [SerializeField]
        bool m_WizardPopupAlreadyShownOnce = false;
        [SerializeField]
        int m_WizardActiveTab = 0;
        [SerializeField]
        bool m_WizardNeedRestartAfterChangingToDX12 = false;
        [SerializeField]
        bool m_WizardNeedToRunFixAllAgainAfterDomainReload = false;
        [SerializeField]
        int m_LastMaterialVersion = k_NeverProcessedMaterialVersion;

        internal const int k_NeverProcessedMaterialVersion = -1;

        public static GameObject defaultScenePrefab
        {
            get => instance.m_DefaultScenePrefabSaved;
            set
            {
                instance.m_DefaultScenePrefabSaved = value;
                Save();
            }
        }

        public static GameObject defaultDXRScenePrefab
        {
            get => instance.m_DefaultDXRScenePrefabSaved;
            set
            {
                instance.m_DefaultDXRScenePrefabSaved = value;
                Save();
            }
        }

        public static string projectSettingsFolderPath
        {
            get => instance.m_ProjectSettingFolderPath;
            set
            {
                instance.m_ProjectSettingFolderPath = value;
                Save();
            }
        }

        public static int wizardActiveTab
        {
            get => instance.m_WizardActiveTab;
            set
            {
                instance.m_WizardActiveTab = value;
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

        public static bool wizardPopupAlreadyShownOnce
        {
            get => instance.m_WizardPopupAlreadyShownOnce;
            set
            {
                instance.m_WizardPopupAlreadyShownOnce = value;
                Save();
            }
        }

        public static bool wizardNeedToRunFixAllAgainAfterDomainReload
        {
            get => instance.m_WizardNeedToRunFixAllAgainAfterDomainReload;
            set
            {
                instance.m_WizardNeedToRunFixAllAgainAfterDomainReload = value;
                Save();
            }
        }

        public static bool wizardNeedRestartAfterChangingToDX12
        {
            get => instance.m_WizardNeedRestartAfterChangingToDX12;
            set
            {
                instance.m_WizardNeedRestartAfterChangingToDX12 = value;
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
    }
}
