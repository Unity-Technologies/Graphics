#if UNITY_EDITOR
using System;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    static class HDProjectSettingsProxy
    {
        public static Func<string> projectSettingsFolderPath;
    }
#endif
}
