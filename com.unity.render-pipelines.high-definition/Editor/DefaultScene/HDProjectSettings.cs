using UnityEngine;
using UnityEditorInternal;
using System.IO;

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
        Version version = Version.First;
#pragma warning restore 414

        [SerializeField]
        GameObject m_DefaultScenePrefabSaved;
        [SerializeField]
        GameObject m_DefaultDXRScenePrefabSaved;
        [SerializeField]
        string m_ProjectSettingFolderPath = "HDRPDefaultResources";
        [SerializeField]
        bool m_WizardPopupAtStart = false;
        [SerializeField]
        int m_WizardActiveTab = 0;
        [SerializeField]
        string m_PackageVersionForMaterials = k_PackageFirstTimeVersionForMaterials;

        internal const string k_PackageFirstTimeVersionForMaterials = "NeverSaved";

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

        internal static int wizardActiveTab
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

        public static string packageVersionForMaterialUpgrade
        {
            get => instance.m_PackageVersionForMaterials;
            set
            {
                instance.m_PackageVersionForMaterials = value;
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
