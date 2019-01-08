using UnityEngine;
using UnityEditorInternal;
using System.IO;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
        string m_ProjectSettingFolderPath = "HDRPDefaultResources";
        [SerializeField]
        bool m_PopupAtStart = false;

        public static GameObject defaultScenePrefab
        {
            get => instance.m_DefaultScenePrefabSaved;
            set
            {
                instance.m_DefaultScenePrefabSaved = value;
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

        public static bool hasStartPopup
        {
            get => instance.m_PopupAtStart;
            set
            {
                instance.m_PopupAtStart = value;
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
