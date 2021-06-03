using UnityEngine;
using UnityEditorInternal;
using System.IO;

namespace UnityEditor.Rendering.HighDefinition
{
    //As ScriptableSingleton is not usable due to internal FilePathAttribute,
    //copying mechanism here
    class HDUserSettings : ScriptableObject
    {
        const string filePath = "UserSettings/HDRPUserSettings.asset";

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

        //singleton pattern
        static HDUserSettings s_Instance;
        static HDUserSettings instance => s_Instance ?? CreateOrLoad();
        HDUserSettings()
        {
            s_Instance = this;
        }

        static HDUserSettings CreateOrLoad()
        {
            // force loading of HDProjectSetting first: its migration can create and init this file
            int unused = HDProjectSettings.materialVersionForUpgrade;

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
}
