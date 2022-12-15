#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    //Cannot rely on ScriptableSingleton as we can chose to recreate the instance in a non readonly state later.
    //(See ScriptableSingleton constructor)

    //Also this runtime is reaconly access. It is only to be able to access important data early in Runtime assembly
    //E.G.: We need the path where asset are created if we want to load or create a new HDRenderPipelineGlobalSettings asset in HDRenderPipelineGlobalSettings.Ensure
    class HDProjectSettingsReadOnlyBase : ScriptableObject
    {
        public const string filePath = "ProjectSettings/HDRPProjectSettings.asset";

        [SerializeField]
        protected string m_ProjectSettingFolderPath = "HDRPDefaultResources";

        public static string projectSettingsFolderPath => instance.m_ProjectSettingFolderPath;

        //singleton pattern
        protected static HDProjectSettingsReadOnlyBase s_Instance;
        static HDProjectSettingsReadOnlyBase instance => s_Instance ?? CreateOrLoad();

        protected HDProjectSettingsReadOnlyBase()
        {
            s_Instance = this;
        }

        static HDProjectSettingsReadOnlyBase CreateOrLoad()
        {
            //try load: if it exists, this will trigger the call to the private ctor
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            //else create
            if (s_Instance == null)
            {
                HDProjectSettingsReadOnlyBase created = CreateInstance<HDProjectSettingsReadOnlyBase>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            return s_Instance;
        }
    }
#endif
}
