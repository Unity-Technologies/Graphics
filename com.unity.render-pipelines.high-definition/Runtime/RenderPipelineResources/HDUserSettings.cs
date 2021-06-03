#if UNITY_EDITOR
using UnityEditorInternal;
using System.IO;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    //As ScriptableSingleton is not usable due to internal FilePathAttribute,
    //copying mechanism here
    class HDUserSettings : ScriptableObject
    {
        const string filePath = "UserSettings/HDRPUserSettings.asset";

        // Use a proxy for HDProjectSettings living in editor assembly
        string m_ProjectSettingFolderPath => HDProjectSettingsProxy.projectSettingsFolderPath();

        [SerializeField]
        bool m_WizardPopupAlreadyShownOnce = false;
        [SerializeField]
        int m_WizardActiveTab = 0;
        [SerializeField]
        bool m_WizardNeedRestartAfterChangingToDX12 = false;
        [SerializeField]
        bool m_WizardNeedToRunFixAllAgainAfterDomainReload = false;

        public static int wizardActiveTab
        {
            get => instance.m_WizardActiveTab;
            set
            {
                instance.m_WizardActiveTab = value;
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

        // Use a proxy for HDProjectSettings living in editor assembly
        public static string projectSettingsFolderPath
        {
            get => instance.m_ProjectSettingFolderPath;
        }

        //singleton pattern
        static HDUserSettings s_Instance;
        static HDUserSettings instance => s_Instance ?? CreateOrLoad();
        HDUserSettings()
        {
            s_Instance = this;
        }

        static HDUserSettings CreateOrLoad()
        {
            // Note: HDProjectSettings should load first but it sits in an editor assembly
            // but its instance creation is triggered early on DLL load and this is sufficient.
            // Otherwise we could check if the HDProjectSettingsProxy.projectSettingsFolderPath delegate is null,
            // and force the assembly load otherwise.
            // HDProjectSettings' migration can create and init the HDUserSettings file.

            //try load
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            //else create
            if (s_Instance == null)
            {
                HDUserSettings created = CreateInstance<HDUserSettings>();
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
#endif
}
