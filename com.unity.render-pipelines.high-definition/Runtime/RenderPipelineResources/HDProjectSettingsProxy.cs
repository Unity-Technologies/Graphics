#if UNITY_EDITOR
using System;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    static class HDProjectSettingsProxy
    {
        static Func<string> s_ProjectSettingsFolderPath;

        public static string projectSettingsFolderPath
            => s_ProjectSettingsFolderPath();

        internal static void Init(Func<string> projectSettingsFolderPathGetter)
            => s_ProjectSettingsFolderPath = projectSettingsFolderPathGetter;
    }
#endif
}
